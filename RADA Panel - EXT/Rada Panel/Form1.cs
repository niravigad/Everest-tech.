using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using InstrumentsDrivers;
using System.Net.NetworkInformation;
using System.Threading;
using TestExecutive;
using System.Diagnostics;

namespace Rada_Panel
{
    public partial class Form1 : Form
    {

        #region Variables


        bool CanStatusTest = false;
        Stopwatch Mainsw = new Stopwatch();

        public enum ArduinoOpcode
        {
            DigitalWrite = 1,
            DigitalRead = 2,
            AnalogRead = 3,
        };

        enum PS_STATUS
        {
            OFF,
            ON
        }

        PS_STATUS GEN_STATUS = PS_STATUS.OFF;
        PS_STATUS ZUP_STATUS = PS_STATUS.OFF;


        double current_out = 0;

        bool[] Relays = new bool[12];

        Lambda mylambda = new Lambda();
        //Cisco mysw = new Cisco();
        ZUP myzup;// = new ZUP();
        Arduino myard = new Arduino();
        USBSerial232 myser = new USBSerial232();

        List<TextBox> dcTB = new List<TextBox>();
        private bool state;

        public string SWVer = "1.0";

        string ArdComPort = "";
        string ZupComPort = "";
        string LamComPort = "";
        string SwitchComPort = "";

        int GenBaudrate = 9600;
        int ZupBaudrate = 9600;
        int ArdBaudrate = 115200;
        int SwitchConsoleBaudrate = 115200;


        #endregion

        public Form1(string LambdaCom, string ZupCom, string ArduinoAdd, string mySerialPort)
        {
            InitializeComponent();
            //Set Global Comports:
            ArdComPort = ArduinoAdd;
            ZupComPort = ZupCom;
            LamComPort = LambdaCom;
            SwitchComPort = mySerialPort;
            //

            Text += " " + SWVer;

            myser.Init(mySerialPort, SwitchConsoleBaudrate);

            /* Interfaces Counstructors*/

            /* Interfaces Communication through serial*/

            //LAMBDA
            if (mylambda.Init(LambdaCom, GenBaudrate, 6))
                ledBulb_Gen.Color = Color.Lime;
            else
                ledBulb_Gen.Color = Color.Red;
            mylambda.ReadOutput(out state);
            if (state)
            {
                GEN_STATUS = PS_STATUS.ON;
                GEN_ON_OFF_SW.BackgroundImage = Rada_Panel.Properties.Resources.ON1;
            }
            //

            //ZUP
            myzup = new ZUP(ZupCom, ZupBaudrate, 1);
            if (myzup.Init())
                ledBulb_ZUP.Color = Color.Lime;
            else
                ledBulb_ZUP.Color = Color.Red;
            myzup.ReadOutput(out state);
            if (state)
            {
                ZUP_STATUS = PS_STATUS.ON;
                ZUP_ON_OFF_SW.BackgroundImage = Rada_Panel.Properties.Resources.ON1;
            }
            //


            //ARD
            if (myard.Init(ArduinoAdd, ArdBaudrate, 0))
                ledBulb_ARD.Color = Color.Lime;
            else
                ledBulb_ARD.Color = Color.Red;
            //


            dcTB.Add(textBox_DC_UUT1);
            dcTB.Add(textBox_DC_UUT2);
            dcTB.Add(textBox_DC_UUT3);
            dcTB.Add(textBox_DC_UUT4);

            //SWITCH
            Ping ping = new Ping();
            PingReply pr = ping.Send(textBox_SWITCH_IP.Text);
            if (pr.Status == IPStatus.Success)
            {
                ledBulb_SWITCH.Color = Color.Lime;
            }
            else
            {
                ledBulb_SWITCH.Color = Color.Red;
            }
            //

            CanStatusTest = true;
            Mainsw.Restart();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            try { mylambda.Close(); } catch (Exception Ex) { }
            try { myzup.DisConnect(); } catch (Exception Ex) { }
            try { myard.Close(); } catch (Exception Ex) { }
        }

        #region Button DC Clicks


        private void DC_UUT_TEST_Click(object sender, EventArgs e)
        {

            CanStatusTest = false; // Lock Status Test            

            byte Io = 0;
            string instructions = "";
            string instructionsdis = "";
            if (((Button)sender) == DC_button_1)
            {
                Io = 0;
                instructions = "Connect HR01 to ST connector , set SW2 to ON \n Set switch on HR01 to ON";
                instructionsdis = "Disconnect HR01 from ST connector , set SW2 to OFF \n Set switch on HR01 to OFF";
                MessageBox.Show(instructions);
                mylambda.SetOutput(true);
                Thread.Sleep(1000);
                //
                current_out = 0;
                string currentSTR = "";
                myard.SendCommandToArduino((byte)ArduinoOpcode.AnalogRead, Io, ref currentSTR);
                current_out = double.Parse(currentSTR = ADC2Current(currentSTR));
                if ((current_out < 2.1) && (current_out > 1.6)) // Nominal value is 1.86[A]
                    dcTB[Io].Text = "Pass";
                else
                    dcTB[Io].Text = "Fail";
                //
                mylambda.SetOutput(false);
                MessageBox.Show(instructionsdis);
            }
            else if (((Button)sender) == DC_button_2)
            {
                Io = 1;
                instructions = "Connect HR01 to ST connector , set SW3 to ON \n Set switch on HR01 to ON";
                instructionsdis = "Disconnect HR01 from ST connector , set SW2 to OFF \n Set switch on HR01 to OFF";
                MessageBox.Show(instructions);
                mylambda.SetOutput(true);
                Thread.Sleep(1000);
                //
                current_out = 0;
                string currentSTR = "";
                myard.SendCommandToArduino((byte)ArduinoOpcode.AnalogRead, Io, ref currentSTR);
                current_out = double.Parse(currentSTR = ADC2Current(currentSTR));
                if ((current_out < 2.1) && (current_out > 1.6)) // Nominal value is 1.86[A]
                    dcTB[Io].Text = "Pass";
                else
                    dcTB[Io].Text = "Fail";
                //
                mylambda.SetOutput(false);
                MessageBox.Show(instructionsdis);
            }
            else if (((Button)sender) == DC_button_3)
            {
                Io = 2;
                instructions = "Connect HR01 to ST connector , set SW4 to ON \n Set switch on HR01 to ON";
                instructionsdis = "Disconnect HR01 from ST connector , set SW2 to OFF \n Set switch on HR01 to OFF";
                MessageBox.Show(instructions);
                mylambda.SetOutput(true);
                Thread.Sleep(1000);
                //
                current_out = 0;
                string currentSTR = "";
                myard.SendCommandToArduino((byte)ArduinoOpcode.AnalogRead, Io, ref currentSTR);
                current_out = double.Parse(currentSTR = ADC2Current(currentSTR));
                if ((current_out < 2.1) && (current_out > 1.6)) // Nominal value is 1.86[A]
                    dcTB[Io].Text = "Pass";
                else
                    dcTB[Io].Text = "Fail";
                //
                mylambda.SetOutput(false);
                MessageBox.Show(instructionsdis);
            }
            else if (((Button)sender) == DC_button_4)
            {
                Io = 3;
                instructions = "Connect HR01 to ST connector , set SW5 to ON \n Set switch on HR01 to ON";
                instructionsdis = "Disconnect HR01 from ST connector , set SW2 to OFF \n Set switch on HR01 to OFF";
                MessageBox.Show(instructions);
                mylambda.SetOutput(true);
                Thread.Sleep(1000);
                //
                current_out = 0;
                string currentSTR = "";
                myard.SendCommandToArduino((byte)ArduinoOpcode.AnalogRead, Io, ref currentSTR);
                current_out = double.Parse(currentSTR = ADC2Current(currentSTR));
                if ((current_out < 2.1) && (current_out > 1.6)) // Nominal value is 1.86[A]
                    dcTB[Io].Text = "Pass";
                else
                    dcTB[Io].Text = "Fail";
                //
                mylambda.SetOutput(false);
                MessageBox.Show(instructionsdis);
            }

            #region old
            //current_out = 0;
            //MessageBox.Show(instructions);
            //mylambda.SetOutput(true);
            //Thread.Sleep(1000);
            //mylambda.ReadCurrent(out current_out);

            //if ((current_out < 2.1) && (current_out > 1.6)) // Nominal value is 1.86[A]
            //    dcTB[Index].Text = "Pass";
            //else
            //    dcTB[Index].Text = "Fail";

            //mylambda.SetOutput(false);
            //MessageBox.Show(instructionsdis);
            #endregion



            CanStatusTest = true; // Unlock Status Test
        }

        private void button_GO_SPARE_Click(object sender, EventArgs e)
        {
            CanStatusTest = false; // Lock Status Test

            MessageBox.Show("Connect HR01 to ST connector , set SW8 to ON \n Set switch on HR01 to ON");
            current_out = 0;
            myzup.SetOutput(true);
            Thread.Sleep(1000);
            myzup.ReadCurrent(out current_out);
            if ((current_out < 0.2) && (current_out > 0.16)) // Nominal value is 0.18666[A]
                textBox_DC_SPARE.Text = "Pass";
            else
                textBox_DC_SPARE.Text = "Fail";

            myzup.SetOutput(false);
            MessageBox.Show("Disconnect HR01 from ST connector , set SW8 to OFF \n Set switch on HR01 to OFF");

            CanStatusTest = true; // Unlock Status Test
        }

        #endregion

        #region Button Comm Clicks

        private void UUT1_comm_Click(object sender, EventArgs e)
        {
            MessageBox.Show(" Connect HR02 to ST connector ");
            /*
            1. Enter here Ping test between ETH switch to Router
            2. Enter here link test between Router to ETH switch 

            */

            textBox_Comm_UUT1.Text = selftestJ2();


        }

        private string selftestJ2()
        {

            CanStatusTest = false; // Lock Status Test




            string Result = "Fail";

            Stopwatch sw = new Stopwatch();
            try
            {
                string str = "";
                myser.CleanBuffer();
                sw.Restart();
                while (!str.Contains("switch417280") && !str.Contains("User Name:") && sw.Elapsed.TotalSeconds < 15)
                {
                    str = myser.WriteRead("\r\n");
                }
                sw.Stop();
                if (!str.Contains("switch417280") && sw.Elapsed.TotalSeconds > 15) // Timeout
                {
                    MessageBox.Show("Switch not ready for test yet! try again later");
                }
                else
                {
                    //Go test:
                    str = myser.WriteRead("\r\n");
                    if (str.Contains("User Name:"))
                    {
                        str = myser.WriteRead("rada\n");
                    }
                    Thread.Sleep(500);
                    if (str.Contains("Password:"))
                    {
                        str = myser.WriteRead("Everest!1\n");
                    }
                    Thread.Sleep(1500);
                    if (str.Contains("switch417280"))
                    {
                        str = myser.WriteRead("show interfaces status\n");

                        myser.WriteRead("q"); // quits to cli idle.
                        string[] lines = str.Split('\n');

                        string gi102_line = "";
                        for (int i = 0; i < lines.Length; i++)
                        {
                            if (lines[i].Contains("gi1/0/2"))  // Search for line: "gi1/0/2  1G-Copper    Full    1000  Enabled  Off  Up          Disabled Off    \r\r"
                            {
                                gi102_line = lines[i];
                                break;
                            }
                        }

                        if (gi102_line.Length > 0)
                        {
                            string[] testphrases = gi102_line.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                            if (testphrases.Length > 0)
                            {
                                if (testphrases[6] == "Up")
                                {
                                    Result = "Pass";
                                    MessageBox.Show("Disconnect HR02 from ST connector");
                                }
                                else
                                {
                                    Result = "Fail";
                                }
                            }
                            else
                            {
                                Result = "Fail";
                            }
                        }
                    }


                }
            }
            catch (Exception ex)
            {
                CanStatusTest = true; // Unlock Status Test
                return Result;
            }

            CanStatusTest = true; // Unlock Status Test
            return Result;

        }

        private void UUT2_comm_Click(object sender, EventArgs e)
        {
            MessageBox.Show(" Connect HR02 to ST connector ");
            /*
            1. Enter here Ping test between ETH switch to Router
            2. Enter here link test between Router to ETH switch 

            */

            textBox_Comm_UUT2.Text = selftestJ2();



        }

        private void UUT3_comm_Click(object sender, EventArgs e)
        {
            MessageBox.Show(" Connect HR02 to ST connector ");
            /*
            1. Enter here Ping test between ETH switch to Router
            2. Enter here link test between Router to ETH switch 

            */

            textBox_Comm_UUT3.Text = selftestJ2();

        }

        private void UUT4_comm_Click(object sender, EventArgs e)
        {
            MessageBox.Show(" Connect HR02 to ST connector ");
            /*
            1. Enter here Ping test between ETH switch to Router
            2. Enter here link test between Router to ETH switch 

            */

            textBox_Comm_UUT4.Text = selftestJ2();
        }

        private void UUT_SPARE_comm_Click(object sender, EventArgs e)
        {



        }

        #endregion






        private void GEN_ON_OFF_SW_Click(object sender, EventArgs e)
        {
            CanStatusTest = false; // Lock Status Test


            if (GEN_STATUS == PS_STATUS.OFF)
            {
                try
                {
                    mylambda.SetOutput(true);
                    GEN_STATUS = PS_STATUS.ON;
                    GEN_ON_OFF_SW.BackgroundImage = Rada_Panel.Properties.Resources.ON1;
                }
                catch
                {
                    MessageBox.Show("PS COM ERROR");
                }

            }
            else
            {
                try
                {
                    mylambda.SetOutput(false);
                    GEN_STATUS = PS_STATUS.OFF;
                    GEN_ON_OFF_SW.BackgroundImage = Rada_Panel.Properties.Resources.OFF1;
                }
                catch
                {
                    MessageBox.Show("PS COM ERROR");
                }

            }


            CanStatusTest = true; // Unlock Status Test
        }

        private void ZUP_ON_OFF_SW_Click(object sender, EventArgs e)
        {
            CanStatusTest = false; // Lock Status Test


            if (ZUP_STATUS == PS_STATUS.OFF)
            {
                try
                {
                    myzup.SetOutput(true);
                    ZUP_STATUS = PS_STATUS.ON;
                    ZUP_ON_OFF_SW.BackgroundImage = Rada_Panel.Properties.Resources.ON1;
                }
                catch
                {
                    MessageBox.Show("PS COM ERROR");
                }

            }
            else
            {
                try
                {
                    myzup.SetOutput(false);
                    ZUP_STATUS = PS_STATUS.OFF;
                    ZUP_ON_OFF_SW.BackgroundImage = Rada_Panel.Properties.Resources.OFF1;
                }
                catch
                {
                    MessageBox.Show("PS COM ERROR");
                }

            }


            CanStatusTest = true; // Unlock Status Test
        }



        private void button_GO_UUT1_Current_Click(object sender, EventArgs e)
        {
            CanStatusTest = false; // Lock Status Test


            byte ch_cur_sns = 0;
            if (myard.isOpen)
            {
                string response = "";
                myard.SendCommandToArduino((byte)ArduinoOpcode.AnalogRead, ch_cur_sns, ref response);
                if (response.Length > 1)
                    textBox_UUT1_Current.Text = ADC2Current(response);
            }


            CanStatusTest = true; // Unlock Status Test
        }

        private void button_GO_UUT2_Current_Click(object sender, EventArgs e)
        {
            CanStatusTest = false; // Lock Status Test


            byte ch_cur_sns = 1;
            if (myard.isOpen)
            {
                string response = "";
                myard.SendCommandToArduino((byte)ArduinoOpcode.AnalogRead, ch_cur_sns, ref response);
                if (response.Length > 1)
                    textBox_UUT2_Current.Text = ADC2Current(response);
            }


            CanStatusTest = true; // Unlock Status Test
        }

        private void button_GO_UUT3_Current_Click(object sender, EventArgs e)
        {
            CanStatusTest = false; // Lock Status Test


            byte ch_cur_sns = 2;
            if (myard.isOpen)
            {
                string response = "";
                myard.SendCommandToArduino((byte)ArduinoOpcode.AnalogRead, ch_cur_sns, ref response);
                if (response.Length > 1)
                    textBox_UUT3_Current.Text = ADC2Current(response);
            }

            CanStatusTest = true; // Unlock Status Test
        }

        private void button_GO_UUT4_Current_Click(object sender, EventArgs e)
        {
            CanStatusTest = false; // Lock Status Test

            byte ch_cur_sns = 3;
            if (myard.isOpen)
            {
                string response = "";
                myard.SendCommandToArduino((byte)ArduinoOpcode.AnalogRead, ch_cur_sns, ref response);
                if (response.Length > 1)
                    textBox_UUT4_Current.Text = ADC2Current(response);
            }

            CanStatusTest = true; // Unlock Status Test
        }

        string ADC2Current(string str)
        {            
            double VCC = 4.824;
            double offset = -2.575;//V
            double CurrentFactor = 10; // 0.1V->1A
            str = str.Replace("\r", "").Replace("\n", "");
            str = Math.Abs((VCC * (double.Parse(str) / 1023.0) + offset) * CurrentFactor).ToString("N3");

            return str;
        }



        private void textBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            PasswordForm nf = new PasswordForm();
            nf.ShowDialog();
            if (nf.PasswordOK)
            {
                textBox_SWITCH_IP.ReadOnly = false;
            }
        }

        private void textBox_SWITCH_IP_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (isipok(textBox_SWITCH_IP.Text))
                {
                    textBox_SWITCH_IP.ReadOnly = true;
                    textBox_SWITCH_IP.BackColor = Color.Lime;
                    textBox_SWITCH_IP.ForeColor = Color.Black;
                    textBox_SWITCH_IP.Refresh();
                    Thread.Sleep(500);
                    textBox_SWITCH_IP.BackColor = Color.White;
                    textBox_SWITCH_IP.ForeColor = Color.Black;
                }
                else
                {
                    textBox_SWITCH_IP.BackColor = Color.Red;
                    textBox_SWITCH_IP.ForeColor = Color.White;
                }

            }
        }

        bool isipok(string str)
        {
            try
            {
                string[] nums = str.Split('.');
                if (nums.Length != 4) return false;
                for (int i = 0; i < nums.Length; i++)
                {
                    if (nums[i].Length > 3 || nums[i].Length < 1)
                    {
                        return false;
                    }

                    for (int j = 0; j < nums[i].Length; j++)
                    {
                        if (!char.IsDigit(nums[i][j]))
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }

        }
        private void timer_status_Tick(object sender, EventArgs e)
        {
            if (Mainsw.Elapsed.TotalSeconds > 7 && CanStatusTest && textBox_SWITCH_IP.ReadOnly)
            {
                //SWITCH
                Ping ping = new Ping();
                PingReply pr = ping.Send(textBox_SWITCH_IP.Text);
                if (pr.Status == IPStatus.Success)
                {
                    ledBulb_SWITCH.Color = Color.Lime;
                }
                else
                {
                    ledBulb_SWITCH.Color = Color.Red;
                }
                //
            }
            else if (!textBox_SWITCH_IP.ReadOnly)
            {
                ledBulb_SWITCH.Color = Color.Yellow;
            }

            if (Mainsw.Elapsed.TotalSeconds > 7 && CanStatusTest)
            {

                //lambda
                bool state = false;
                bool isconnected = false;
                isconnected = mylambda.ReadOutput(out state);
                if (isconnected)
                {
                    ledBulb_Gen.Color = Color.Lime;
                    mylambda.ReadOutput(out state);
                    if (state)
                    {
                        GEN_STATUS = PS_STATUS.ON;
                        GEN_ON_OFF_SW.BackgroundImage = Rada_Panel.Properties.Resources.ON1;
                    }
                    else
                    {
                        GEN_STATUS = PS_STATUS.OFF;
                        GEN_ON_OFF_SW.BackgroundImage = Rada_Panel.Properties.Resources.OFF1;
                    }
                }
                else
                {
                    ledBulb_Gen.Color = Color.Red;
                    try // Reconnect..
                    {
                        mylambda.Init(LamComPort, GenBaudrate, 6);

                    }
                    catch
                    {

                    }
                }
                //
            }

            if (Mainsw.Elapsed.TotalSeconds > 7 && CanStatusTest)
            {

                //zup
                bool isconnected = false;
                isconnected = myzup.IsConnected();
                if (isconnected)
                {
                    ledBulb_ZUP.Color = Color.Lime;
                    myzup.ReadOutput(out state);
                    if (state)
                    {
                        ZUP_STATUS = PS_STATUS.ON;
                        ZUP_ON_OFF_SW.BackgroundImage = Rada_Panel.Properties.Resources.ON1;
                    }
                    else
                    {
                        ZUP_STATUS = PS_STATUS.OFF;
                        ZUP_ON_OFF_SW.BackgroundImage = Rada_Panel.Properties.Resources.OFF1;
                    }
                }
                else
                {
                    ledBulb_ZUP.Color = Color.Red;
                    try // Reconnect..
                    {
                        myzup.Init();

                    }
                    catch
                    {

                    }
                }
                //
            }

            if (Mainsw.Elapsed.TotalSeconds > 7 && CanStatusTest)
            {
                //myard
                string resp = "";
                bool state = false;
                state = myard.SendCommandToArduino((byte)ArduinoOpcode.AnalogRead, 0, ref resp);
                if (state)
                {
                    ledBulb_ARD.Color = Color.Lime;
                }
                else
                {
                    ledBulb_ARD.Color = Color.Red;
                    try // Reconnect..
                    {
                        myard.Init(ArdComPort, ArdBaudrate, 0);
                    }
                    catch
                    {

                    }
                }

                //
            }

        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            textBox_SWITCH_IP.Focus();
        }

        private void Form1_MouseClick(object sender, MouseEventArgs e)
        {
            Mainsw.Restart();
        }
    }



}


