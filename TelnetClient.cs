using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using RTS.Gen;
using RTS.Gen.Connections;
using RTS.Gen.Common;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace RTS.Infra.Connections
{    
    /// <summary>
    /// General Telnet class to communication with devices.
    /// Support two working mode:
    ///     Normal:         Sync Read and Write operations. Read blocks if no data avaliable.
    ///     BuferedRead:    Sync Read and Write operations. Reads are buffered locally. Read blocks if no data avaliable.
    /// </summary>
    public class CTelnetClient : CIPConnection , ICLI , IAsyncConnection, IRestartable {

    #region Public
        public enum EMode { Normal, BufferedRead}
        private const int TELNET_SOCKET = 23;
        private const string END_INDICATOR = "-> ";
        #region Methods
        /// <summary>
        /// Constracting new CTelnetClient.
        /// </summary>        
        /// <param name="ip"> destination's ip.</param>
        /// <param name="user"> user name for login</param>
        /// <param name="password"> password for login</param>
        /// <param name="mode"> Working mode.</param>
        /// <remarks>
        /// If working mode is EMode.BufferedRead, a new thread will be opened, and all readed data will be buffered till
        /// Read() is called. If working mode is Emode.Normal, no thread will opened, and calling Read() will return the buffered
        /// data in the socket.
        /// </remarks>
        public CTelnetClient(EMode mode, string ip, string user, string password, params string[] startUpCmds) {
            m_ip = ip;
            m_userName = user;
            m_password = password;
            m_mode = mode;
            m_startUpCmds = startUpCmds;
            m_iSocketPort = TELNET_SOCKET;
            m_EndIndicator = END_INDICATOR;
            RestartMembers();
        }

        public CTelnetClient(EMode mode, string ip, int Socket, string user, string password, params string[] startUpCmds)
        {
            m_ip = ip;
            m_userName = user;
            m_password = password;
            m_mode = mode;
            m_startUpCmds = startUpCmds;
            m_iSocketPort = Socket;
            m_EndIndicator = END_INDICATOR;
            RestartMembers();
        }

        /// <summary>
        /// Restarting members state.
        /// </summary>
        private void RestartMembers() {
            m_tcpClient = new TcpClient();            
            m_isDisposed = false;                    
            m_data = new StringBuilder();            
            m_asyncData = new byte[4096];
            m_asyncBuffer = new StringBuilder();
            m_tcpStack = new TCPStack();
        }

        /// <summary>
        /// Restarting telnet connection.
        /// </summary>
        /// <remarks>
        /// The old socket will be close and a new one will be opened.         
        /// </remarks>        
        public void Restart() {
            Log("Restarting...");
            if(m_tcpStack.SourceAddress != null)
                CPcapClient.SendFin(m_tcpStack);

            // Close telnet. If there is a reading thread wait for it to close too.
            Dispose();
            if (m_mode == EMode.BufferedRead)
                m_thread.Join();            

            // Restart object state.            
            RestartMembers();
            MiliSleep(1000);
            Init();

            // Connect again.
            Login(30000);

            // Restore calls to BeginRead that were on last socket.
            RestoreBeginRead();
        }
        
        /// <summary>
        /// Initializing.
        /// </summary>
        /// <returns>True if initialize successfully, else false.</returns>
        public bool Init() {
            m_socket_identifier = eSocketIdentifier.Undefied;
            if (m_mode == EMode.BufferedRead) {
                m_thread = new Thread(this.ThreadProc);
                m_threadContinue = true;
            }            
            return true;
        }

        ~CTelnetClient() {         
            Dispose(false);
        }

        /// <summary>
        /// performing login process.        
        /// </summary>
        ///<param name="timeout"> max miliseconds to wait for device's response. If timeout is
        /// reached a TimeoutException is thrown.</param>
        /// <returns>Last string read from device.</returns>
        /// <exception> <see cref="TcpClient"/> </exception>
        /// <exception cref="RTSTelnetException"> Trows if login process failed.</exception>                
        public bool Login(int timeout = 5000)
        {

            // Connect to destination. 
            try
            {
                var result = m_tcpClient.BeginConnect(m_ip, m_iSocketPort, null, null);
                bool success = false;
                Task waitForConnection = Task.Factory.StartNew(() => { success = result.AsyncWaitHandle.WaitOne(timeout); }, SharedCancellation);
                Task.WaitAny(waitForConnection);

                // Cancellation requested
                if (RtsRunCancelled)
                    return false;

                // Catch exception from AsyncWaitHandle.WaitOne
                if (waitForConnection.Exception != null)
                    throw waitForConnection.Exception;

                if (!success) {
                    throw new RTSTelnetException(m_name, "Telnet reached timeout, connecting to: " + m_ip);
                }

                // we have connected
                m_tcpClient.EndConnect(result);
                //m_tcpClient.Connect(m_ip, 23);
            } catch {
                RTSTelnetException exception = new RTSTelnetException(m_name, String.Format("Socket connect timeout to {0}", m_ip));
                throw exception;
            }

            // Start reading thread.
            if (m_mode == EMode.BufferedRead)
                m_thread.Start(Thread.CurrentThread);

            if (m_iSocketPort == TELNET_SOCKET)
            {

                // wait for login prompt, write user.         
                MiliSleep(100);
                Read("login:", true, timeout);
                WriteLine(m_userName);

                // wait for password prompt, write password.
                Read("password:", true, timeout);
                WriteLine(m_password);

                // wait for prompt.      
                try
                {
                    string response = "";
                    while (!(response += Read(timeout)).ToLower().Contains(m_EndIndicator)) ;
                }
              catch(Exception e)
                {
                    RTSTelnetException exception = new RTSTelnetException(m_name, "Login error :" + e.Message);
                    throw exception;
                }

            }
            else
            {
                try
                {
                    string response = "";
                    while (!(response += Read(timeout)).ToLower().Contains(m_EndIndicator)) ;
                }
                catch(Exception e)
                {
                    RTSTelnetException exception = new RTSTelnetException(m_name, "Login error :" + e.Message);
                    throw exception;
                }
            }

            // Get tcp stack for internal managment.
            int retires = 0;
            do
            {
                if (retires > 0)
                    MiliSleep(1000);
                m_tcpStack = CPcapClient.GetTcpStack(m_tcpClient.Client);
                retires++;
            }
            while (m_tcpStack == null && retires < 10) ;
           
            if (m_tcpStack == null)
                throw new RTSTelnetException(m_name, "Stack exception,could not get TcpStack");

            //To Get PcapClient Sends /n , we read reply  the reply before continuing. 
            try
            {
                for (int i = 0; i < retires; i++)
                {
                    Read(100);
                }
            }
            catch { }

            //cleanup excessive /n given in startup
            try
            {
                for (int i = 0; i < 3; i++)
                {
                    Read(100);
                    Log("DEBUG: where was an exssive enter in login");
                }
            }
            catch { }

            // Perform startup cmds
            PerformStartUpCmds();

            try {
                Read(100);
            }
            catch { }

            return true;
        }    

        /// <returns> <see cref="TcpClient.Connected"/></returns>
        public bool IsConnected() {
            if (m_tcpClient != null)
                return m_tcpClient.Connected;
            return false;
        }

        public void PrintPacketInfo() {
            Log(String.Format("Sequence:{0} Ack:{1}", m_tcpStack.SequenceNumber, m_tcpStack.AcknowledgmentNumber));
        }

        /// <summary>
        /// Returns data read form the socket. Blocks if no data avaliable.
        /// </summary>
        /// <param name="timeout"> max time in milisecond to block before throwing TmeoutException. 
        /// if timeout is zero the method return imidialty.</param>
        /// <returns> Data read form the socket</returns>
        /// <exception cref="System.Net.Sockets.SocketException"> throws when connection failed or timeout.</exception>        
        public string Read(int timeout = Timeout.Infinite) {
            return Read(m_EndIndicator, false, timeout);
        }

        /// <summary>
        /// Returns data read form the socket till incountering endIndicator. 
        /// </summary>
        /// <param name="endIndicator">Indicates end of string. If equals to string.empty this method will returns
        /// <param name="ignoreCase">If True case insensetive comparsion will be used for searching end indicator</param>        
        /// with the first data avaliable in the socket.</param>
        /// <param name="timeout"> max time in milisecond to block before throwing TmeoutException. </param>        
        /// <returns> Data read form the socket till incountering endIndicator</returns>
        /// <exception cref="IOException"> throws when connection failes or timeout.</exception>        
        public string Read(string endIndicator, bool ignoreCase = false, int timeout = Timeout.Infinite) {            
            string retStr = "";

            // Check connection first.
            if (!IsConnected())
                throw new RTSTelnetException(m_name, "Socket not connected");

            // Read according to mode.
            try {
                switch (m_mode) {
                    case EMode.Normal:
                        int bytesRead;
                        retStr = ReadFromStream(m_tcpClient.GetStream(), out bytesRead, endIndicator, ignoreCase, timeout);
                        break;
                    case EMode.BufferedRead:
                        retStr = ReadBuffered(endIndicator, ignoreCase, timeout);
                        break;
                }
            } catch (IOException e) {                
                throw new RTSTelnetException(m_name, String.Format("Read timeout after {0}. Telnet is no longer avaliable", timeout), e);
            }

            if (!m_bPrintCommandOutputToLog && (m_sCommand == "PRINT_PTP_TRACE" || m_sCommand == "PrintIsrTiming"))
            {
                Log(m_name, m_sCommand + ": not printing to reduce file size - see TelnetClient.cs", CLogManager.EDirection.In);
            }
            else
            {
                Log(m_name, retStr, CLogManager.EDirection.In);
            }
            return retStr;
        }

        /// <param name="str"> string to write to the socket.</param>
        public void Write(string str)
        {
            try
            {


                byte[] buf = System.Text.ASCIIEncoding.ASCII.GetBytes(str);

                Log(m_name, str, CLogManager.EDirection.Out);
                m_tcpStack.Sent(buf);

                m_tcpClient.GetStream().Write(buf, 0, buf.Length);
            }
            catch (IOException e)
            {
                throw new RTSTelnetException(m_name, String.Format("Write Command Failed . Telnet is no longer avaliable - inner message : {0} ", e.Message), e);

            }
        }

        /// <param name="str"> string to write to the socket.</param>        
        public void WriteLine(string str) {
            Write(str + "\n");
        }

        /// <summary>
        /// Write to the socket and wait for response. Response is recognize if it ends with prompt "-> ".
        /// </summary>
        /// <param name="data"> data to write.</param>
        /// <param name="timeout"> Max time to wait for response in miliseconds. For deafult <see cref="RTS.Properties.Settings.Default.TelnetCMDTimeout"/></param>
        /// <returns> resposne from destination.</returns>
        /// <exception cref="System.TimeoutException"> throws when timeout occures</exception>                
        public string Cmd(string data, int timeout = -1) 
        {
            m_sCommand = data;
            WriteLine(data);

            // Set default timeout.
            if (timeout == -1)
                timeout = RtsDefaults.TELNET_CMD_TIMEOUT;

            return Read(m_EndIndicator, false, timeout);            
        }

        /// <summary>
        /// Return Connection Identifier (Carrier Index . Common , Carrier1, Carrier2)
        /// </summary>
        /// <returns></returns>
        public eSocketIdentifier GetConnnectionIdentifier()
        {
           return this.m_socket_identifier;
        }

        /// <summary>
        /// Must be called to release resources.
        /// </summary>
        public override void Dispose() {
            Dispose(true);
            // Already dispose, no reasone to finalize.
            GC.SuppressFinalize(this);
        }        

        /// <summary>
        /// Wrapper for Radwin commands
        /// </summary>
        /// <param name="cmd">command type (no params)</param>
        public void WriteLine(ERadwinCmd cmd) {
            WriteLine(CRadwinCli.Format(cmd));
        }

        /// <summary>
        /// Wrapper for Radwin commands
        /// </summary>
        /// <param name="cmd">command type</param>
        /// <param name="param">parameter to command</param>
        public void WriteLine(ERadwinCmd cmd, string param) {
            WriteLine(CRadwinCli.Format(cmd, param));
        }

        /// <summary>
        /// Wrapper for Radwin commands
        /// </summary>
        /// <param name="cmd">command type (no params)</param>
        public string Cmd(ERadwinCmd cmd) {
            return Cmd(CRadwinCli.Format(cmd));
        }

        /// <summary>
        /// Wrapper for Radwin commands
        /// </summary>
        /// <param name="cmd">command type</param>
        /// <param name="param">parameter to command</param>
        public string Cmd(ERadwinCmd cmd, string param) {
            return Cmd(CRadwinCli.Format(cmd, param));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="param"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public string Cmd(string data, string param, int timeout = Timeout.Infinite) { return Read(); }

        /// <summary>
        /// Start asynchronous read process.
        /// </summary>
        /// <param name="callback">Called when data is ready to be read.</param>        
        public void BeginRead(AsyncCallback callback) {
            if (!m_isDisposed) {
                m_asyncResult = m_tcpClient.GetStream().BeginRead(m_asyncData, 0, m_asyncData.Length, callback, this);

                // Saved for restore when restarting the telnet.
                m_callerCallback = callback;
            }
        }

        /// <summary>
        /// Ends asynchronous read process.
        /// </summary>
        /// <remarks>
        /// Must be called after BeginRead.
        /// The string read from the socket is buffered internally and returned only if it ends with "\n".
        /// If this methods is called and "\n" is yet to arrive, an empty string is returned.
        /// </remarks>
        /// <returns>String read from the socket, or empty string.</returns>        
        public string EndRead() {
            string response = "";

            if (!m_isDisposed) {
                // Read data
                int bytesRead = m_tcpClient.GetStream().EndRead(m_asyncResult);
                if (bytesRead > 0)
                    m_asyncBuffer.Append(Encoding.ASCII.GetString(m_asyncData, 0, bytesRead));
                m_tcpStack.Received(m_asyncData, bytesRead);

                // Data read successfully, no need to save caller's callback for resturation anymore.
                m_callerCallback = null;

                // Buffer data if it doest ends with "\n".
                response = m_asyncBuffer.ToString();

                // find the last full line index
                int lastLineIndex = response.LastIndexOf("\n");
                if (lastLineIndex != -1) {

                    // store the rest of unfinished line for future
                    string responseRemainder = response.Substring(lastLineIndex + 1);
                    m_asyncBuffer.Clear();
                    m_asyncBuffer.Append(responseRemainder);

                    // return all completed lines
                    response = response.Substring(0, lastLineIndex + 1);
                } 
                else
                    response = "";                
            }
            return response;
        }        

        #endregion

        #region Members
        /// <summary>
        /// User name to use while logging in.
        /// </summary>
        public string m_userName { get; private set; }

        /// <summary>
        /// Password to use while loggin in.
        /// </summary>
        public string m_password { get; private set; }                 
        #endregion
    #endregion

    #region Protected
        #region Methods        
        /// <summary>
        /// Must be called to release resources.
        /// </summary>
        /// <param name="disposing">If true despose of managed resources as well</param>
        protected virtual void Dispose(bool disposing) {
            if (!this.m_isDisposed) {

                // Note disposing has been done.
                m_isDisposed = true;
                
                // Dispose unmanaged.  
                m_threadContinue = false;

                // close socket.
                if (m_tcpClient != null) {
                    //if (m_tcpClient.Connected)
                      //  m_tcpClient.GetStream().Close();
                    m_tcpClient.Close();

                    // if other threads sleeps on m_data wake them all up.                    
                    lock (m_data) {
                        Monitor.PulseAll(m_data);
                    }
                }

                if (disposing) {
                    // Dispose managed.                    
                }                
            }
        }    
        #endregion        
    #endregion

    #region Private
        #region Methods
        /// <summary>
        /// Filtering all telnet commands from a given byte array.
        /// </summary>
        /// <param name="array">array to filter</param>
        /// <param name="arraySize">array size</param>
        /// <param name="filtered">new byte array with no telnet commands in it</param>
        /// <returns>size of filtered array</returns>
        private static int filterTelnetCmd(byte[] array, int arraySize, byte[] filtered) {
            int pos = 0;
            for (int i = 0; i < arraySize && pos < filtered.Length; i++) {
                if (array[i] == 0xff)
                    i += 2;
                else {
                    filtered[pos] = array[i];
                    pos++;
                }
            }
            return pos;
        }

        /// <summary>
        /// Reads in Buffered working mode.
        /// </summary>
        /// <param name="timeout">Timeout for blocking.</param>
        /// <returns>string read from socket.</returns>
        private string ReadBuffered(int timeout) {
            return ReadBuffered(string.Empty, false, timeout);
        }

        /// <summary>
        /// Reads in Buffered working mode, till a specific string appears.
        /// </summary>
        /// <param name="endIndicator">Indicates end of string. If equals to string.empty this method will returns
        /// with the first data avaliable in the socket.</param>
        /// <param name="timeout">Timeout for blocking if no end indicator found.</param>
        /// <param name="ignoreCase">If True case insensetive comparsion will be used for searching end indicator</param>        
        /// <returns>String read from socket.</returns>
        private string ReadBuffered(string endIndicator, bool ignoreCase, int timeout) {
            string retStr = "";
            StringComparison comp = StringComparison.Ordinal;
            if (ignoreCase)
                comp = StringComparison.OrdinalIgnoreCase;

            lock (m_data) {
                // block while no data or not contains endIndicator
                int index = 0;
                while ((m_data.Length == 0 || ((index = m_data.ToString().IndexOf(endIndicator, comp)) == -1)) && m_tcpClient.Connected)
                    WaitForData(timeout);

                retStr = m_data.ToString();
                if (endIndicator != string.Empty) {
                    // Return data till endIndicator only, leave the rest in m_data.
                    retStr = retStr.Substring(0, index + endIndicator.Length);
                    m_data.Remove(0, index + endIndicator.Length);
                } else {
                    // Return whole data.
                    m_data.Clear();
                }
            }
            return retStr;
        }

        /// <summary>
        /// Reading all avaliable data from s network stream. Blocks if no data avaliable.        
        /// </summary>
        /// <param name="stream"> Stream to read from.</param>
        /// <param name="bytesRead">Number of bytes read</param>
        /// <param name="timeout">Read timeout in miliseconds.</param>
        /// <returns>Data read from stream in ASCII string format.</returns>
        /// <exception cref="IOException">If socket is closed, or timeout reached.</exception>
        private string ReadFromStream(NetworkStream stream, out int bytesRead, int timeout = Timeout.Infinite) {
            StringBuilder retStr = new StringBuilder();
            bytesRead = 0;

            // Set timeout            
            stream.ReadTimeout = timeout;

            // While there is still data in the socket.
            do {
                // Read data from socket
                byte[] buffer = new byte[8192];
                int numOfBytes = stream.Read(buffer, 0, buffer.Length);

                // Update tcp stack
                m_tcpStack.Received(buffer, numOfBytes);

                // Filter telnet commands
                byte[] filtered = new byte[numOfBytes];
                numOfBytes = filterTelnetCmd(buffer, numOfBytes, filtered);

                // Convert data to ascii
                if (numOfBytes > 0)
                    retStr.Append(Encoding.ASCII.GetString(filtered).Substring(0, numOfBytes));
                bytesRead += numOfBytes;
            } while (stream.DataAvailable);

            return retStr.ToString();
        }

        /// <summary>
        /// Reading all avaliable data from s network stream, till a specific end indicator. Blocks if no data avaliable.        
        /// </summary>
        /// <param name="stream">stream to read from</param>
        /// <param name="bytesRead">Number of bytes read</param>
        /// <param name="timeout">Read timeout in miliseconds.</param>
        /// <param name="endIndicator">String to wait for which symbols end of data.</param>
        /// <param name="comp">Comparsion to use when searching for end indicator.</param>
        /// <returns>Data read from stream in ASCII string format.</returns>
        private string ReadFromStream(NetworkStream stream, out int bytesRead, string endIndicator, StringComparison comp, int timeout = Timeout.Infinite) {

            int l_bytesRead = 0;
            bytesRead = 0;
            int index;
            Stopwatch timeoutTimer = new Stopwatch();
            timeoutTimer.Start();
            while ((index = m_data.ToString().IndexOf(endIndicator, comp)) == -1 && timeoutTimer.ElapsedMilliseconds < (timeout + 1000))
            {
                m_data.Append(ReadFromStream(stream, out l_bytesRead, timeout));
                bytesRead += l_bytesRead;
            }
            if (index == -1)
                throw new RTSConnectionException("Telnet","Timeout  : " + m_data.ToString());
            string retStr = m_data.ToString();
            if (endIndicator != string.Empty)
            {
                m_data.Clear();
                m_data.Append(retStr.Substring(index + endIndicator.Length));
                retStr = retStr.Substring(0, index + endIndicator.Length);
            }
                        
            return retStr;
        }

        /// <summary>
        /// Reading all avaliable data from s network stream, till a specific end indicator. Blocks if no data avaliable.        
        /// </summary>
        /// <param name="stream">stream to read from</param>
        /// <param name="bytesRead">Number of bytes read</param>
        /// <param name="timeout">Read timeout in miliseconds.</param>
        /// <param name="endIndicator">String to wait for which symbols end of data.</param>
        /// <param name="ignoreCase">If True case insensetive comparsion will be used for searching end indicator</param>        
        /// <returns></returns>
        private string ReadFromStream(NetworkStream stream, out int bytesRead, string endIndicator, bool ignoreCase = false, int timeout = Timeout.Infinite) {
            StringComparison comp = StringComparison.Ordinal;
            if (ignoreCase)
                comp = StringComparison.OrdinalIgnoreCase;
                                        
            return ReadFromStream(stream, out bytesRead, endIndicator, comp, timeout);
        }

        /// <summary>
        /// Listening to the socket in an infinit loop and puts data in m_data.
        /// In case of exception abort the creator thread with the exception as information.
        /// </summary>
        /// <param name="creator"> creator thread.</param>
        private void ThreadProc(object creator) {
            try {
                int bytesRead;
                while (m_threadContinue) {
                    // Read data from stream
                    string data = ReadFromStream(m_tcpClient.GetStream(), out bytesRead);                                        

                    // Store data.
                    if (bytesRead > 0) {
                        lock (m_data) {
                            m_data.Append(data);
                            Monitor.Pulse(m_data); // Wakeup reading thread.
                        }
                    }
                }
            } catch (System.Exception e) {
                if (m_threadContinue) {
                    // Abort creator only if exception is not the result of calling desposne.
                    ((Thread)creator).Abort(e);
                }
            }
        }

        /// <summary>
        /// blocking on m_data till pulse arrives from another thread.
        /// NOTE: caller MUST have locked m_data before calling this method.        
        /// </summary>
        /// <param name="timeout"> time to wait before throwing TimeoutException</param>
        /// <exception cref="IOException"> throws when timeout occures</exception>
        private void WaitForData(int timeout) {
            if (!Monitor.Wait(m_data, timeout)) { // wait till pulsed or timeout.
                if (!m_tcpClient.Connected)
                    throw new IOException("Socket not connected");
                else
                    throw new IOException(String.Format("Wait for data timeout after {0}",timeout));
            }
        }

        /// <summary>
        /// Perfoming all Cmds given at the constructor.
        /// </summary>
        private void PerformStartUpCmds() {

            /*Read Process ID for each socket connection*/
            // PrintProcessId get command result
            // Command Return current Socket ID and supported only for T1042 Units   
            // Options: 0 - Common , 1 - Socket(Carrier1) , 2 - Socket(Carrier2)
            string reply = Cmd(CRadwinCli.Format(ERadwinCmd.PRINT_PROCESS_ID));
            Log($"Print Proccess ID Reply = {reply}");
            Match match = Regex.Match(reply, @"The process Id is =(?<result>\d)");
            if (match.Groups.Count > 1) {
                this.m_socket_identifier = (eSocketIdentifier)Int32.Parse(match.Groups["result"].Value);
            }
            else {
                Log("Process Is UnIdentified!");
                this.m_socket_identifier = (eSocketIdentifier)eSocketIdentifier.Undefied;
            }



            foreach (var cmd in m_startUpCmds)
                Cmd(cmd);
   
        }

        /// <summary>
        /// Restores previos call on BeginRead().        
        /// </summary>
        /// <remarks>
        /// Will be restored only if EndRead() hadnt been called yet.        
        /// This method should be called when restarting the connection.
        /// </remarks>
        private void RestoreBeginRead() {
            if (m_tcpClient.Connected && m_callerCallback != null) {
                BeginRead(m_callerCallback);
                Log("Restored BeginRead() call");
            }
        }

        #endregion
        #region Members
        /// <summary> 
        /// Track whether Dispose has been called.
        /// </summary>         
        private volatile bool m_isDisposed;

        /// <summary>
        /// While true ThreadProc() will continue to run.
        /// </summary>
        private volatile bool m_threadContinue;

        /// <summary>
        /// Used for sending and receiving data
        /// </summary>
        public TcpClient m_tcpClient {get; private set;}

        /// <summary>
        /// Contains data read from the socket.
        /// This is a shared resource by two threads.        
        /// </summary>
        private StringBuilder m_data = new StringBuilder();

        /// <summary>
        /// Thread for reading incoming data and isert it to m_readedData.
        /// </summary>
        private Thread m_thread;

        /// <summary>
        /// Working mode with this instance.
        /// </summary>
        private EMode m_mode;
        
        /// <summary>
        /// Working Socket Port (By Default Telnet Socket) 
        /// </summary>
        private int m_iSocketPort;
		
		/// <summary>
        /// EndIndicator Holderf 
        /// </summary>
        private string m_EndIndicator;

        /// <summary>
        /// member for saving the command name - for PRINT_PTP_TRACE or PrintIsrTiming commands
        /// </summary>
        public string m_sCommand;

        /// <summary>
        /// member for printing or not the command PRINT_PTP_TRACE or PrintIsrTiming output to log file (to reduce log file size)
        /// </summary>
        public bool m_bPrintCommandOutputToLog = false;

        /// <summary>
        /// Data when using async reads and writes.
        /// </summary>
        private byte[] m_asyncData;

        /// <summary>
        /// Buffered async data when using async reads and writes.
        /// </summary>
        private StringBuilder m_asyncBuffer;

        /// <summary>
        /// Result of async call to BeginRead.
        /// </summary>
        private IAsyncResult m_asyncResult;

        /// <summary>
        /// Callback method given by the caller to BeginRead.         
        /// </summary>
        private AsyncCallback m_callerCallback;

        /// <summary>
        /// Contains information about tcp connection.        
        /// </summary>
        private TCPStack m_tcpStack;

        /// <summary>
        /// Cmds to perform after login.
        /// </summary>
        private string[] m_startUpCmds;                
        #endregion
    #endregion       
    }    
}
