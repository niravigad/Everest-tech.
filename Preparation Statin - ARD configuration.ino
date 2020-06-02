//inputs
#define CH1_CNT_ON_OFF  2
#define CH2_CNT_ON_OFF  3
#define CH3_CNT_ON_OFF  4
#define CH4_CNT_ON_OFF  5
#define CH5_CNT_ON_OFF  6
//#define CH6_CNT_ON_OFF  7
//#define CH7_CNT_ON_OFF  8

//outputs

//analog
#define CH1_CUR_SNS A0
#define CH2_CUR_SNS A1
#define CH3_CUR_SNS A2
#define CH4_CUR_SNS A3
#define CH5_CUR_SNS A4
#define CH6_CUR_SNS A5


//protocol

byte X_Header = 0xBB;
byte X_Footer = 0xCC;
byte ByteArray[30]={0};
int ArrayIndex=0;
boolean FindHeader=false;
boolean FindFooter=false;
boolean MsgComplete = false;  // whether the string is complete
const int Opcode = 1;
const int Address = 2;
const int Value = 3;
const int RelayPosition = 4;
const int X_Write = 1;
const int X_Read = 2;
const int X_Relay = 3;
const int X_Analog_Read = 5;
const int X_Set_Port = 5;

//functions
void SetSwitch (int Switch,int state);

void setup() {
  
//digital

pinMode(CH1_CNT_ON_OFF, OUTPUT);
pinMode(CH2_CNT_ON_OFF, OUTPUT);
pinMode(CH3_CNT_ON_OFF, OUTPUT);
pinMode(CH4_CNT_ON_OFF, OUTPUT);
pinMode(CH5_CNT_ON_OFF, OUTPUT);


//analog

pinMode(CH1_CUR_SNS, INPUT);
pinMode(CH2_CUR_SNS, INPUT);
pinMode(CH3_CUR_SNS, INPUT);
pinMode(CH4_CUR_SNS, INPUT);
pinMode(CH5_CUR_SNS, INPUT);

  
  Serial.begin(9600); 
}

int k=0;


void loop()
{
  int temp=0;
  int Relay=0;
  int sensorValue = 0;
  float voltage=0;
   
     while (Serial.available()) 
      {        
              // Serial.println("barak");
              // get the new byte:              
              byte inChar = Serial.read(); 
              //Serial.println(inChar);
              
              // add it to the inputString:
              //inputString += inChar;
              // if the incoming character is a newline, set a flag
              // so the main loop can do something about it:
              if (inChar == X_Header)
              {
                   //Serial.println("header");
                    ArrayIndex=0;
                    FindHeader = true;
              } 
              if(FindHeader)
              {
                     ByteArray [ArrayIndex++] = inChar;
              }
              if (inChar == X_Footer)
              {
                    //Serial.println("footer");
                    FindFooter = true;                    
              } 
              
              if( FindHeader && FindFooter)
              {
                      //Serial.println("both");
                      MsgComplete = true;
                      FindHeader=false;
                      FindFooter=false;
              }
      }
      
      if (MsgComplete) 
      {
        //Serial.println("msg complete");
        switch(ByteArray[Opcode])
        {
          case X_Write:
             if(ByteArray[Address]==100)
            {
                digitalWrite(A0, ByteArray[Value]);
            }
            else if(ByteArray[Address]==101)
            {
                digitalWrite(A1, ByteArray[Value]);
            }
            else if(ByteArray[Address]==102)
            {
                digitalWrite(A2, ByteArray[Value]);
            }
            else if(ByteArray[Address]==103)
            {
                digitalWrite(A3, ByteArray[Value]);
            }
            else if(ByteArray[Address]==104)
            {
                digitalWrite(A4, ByteArray[Value]);
            }
            else if(ByteArray[Address]==105)
            {
                digitalWrite(A5, ByteArray[Value]);
            }
            else if(ByteArray[Address]==106)
            {
                digitalWrite(A6, ByteArray[Value]);
            }
            else if(ByteArray[Address]==107)
            {
                digitalWrite(A7, ByteArray[Value]);
            }         
            else if(ByteArray[Address]==108)
            {
                digitalWrite(A8, ByteArray[Value]);
            }
            else if(ByteArray[Address]==109)
            {
                digitalWrite(A9, ByteArray[Value]);
            }
            else if(ByteArray[Address]==110)
            {
                digitalWrite(A10, ByteArray[Value]);
            }
            else if(ByteArray[Address]==111)
            {
                digitalWrite(A11, ByteArray[Value]);
            }
            else if(ByteArray[Address]==112)
            {
                digitalWrite(A12, ByteArray[Value]);
            }
            else if(ByteArray[Address]==113)
            {
                digitalWrite(A13, ByteArray[Value]);
            }
            else if(ByteArray[Address]==114)
            {
                digitalWrite(A14, ByteArray[Value]);
            }
            else if(ByteArray[Address]==115)
            {
                digitalWrite(A15, ByteArray[Value]);
            }
            else
            {
              digitalWrite(ByteArray[Address], ByteArray[Value]);
            }
            Serial.println("ACK");
            
          break;
          case X_Read:
          if(ByteArray[Address]==100)
            {
                temp = digitalRead(A0);
            }
            else if(ByteArray[Address]==101)
            {
                temp = digitalRead(A1);
            }
            else if(ByteArray[Address]==102)
            {
                temp = digitalRead(A2);
            }
            else if(ByteArray[Address]==103)
            {
                temp = digitalRead(A3);
            }
            else if(ByteArray[Address]==104)
            {
                temp = digitalRead(A4);
            }
            else if(ByteArray[Address]==105)
            {
                temp = digitalRead(A5);
            }
            else if(ByteArray[Address]==106)
            {
                temp = digitalRead(A6);
            }
            else if(ByteArray[Address]==107)
            {
                temp = digitalRead(A7);
            }         
            else if(ByteArray[Address]==108)
            {
                temp = digitalRead(A8);
            }
            else if(ByteArray[Address]==109)
            {
                temp = digitalRead(A9);
            }
            else if(ByteArray[Address]==110)
            {
                temp = digitalRead(A10);
            }
            else if(ByteArray[Address]==111)
            {
                temp = digitalRead(A11);
            }
            else if(ByteArray[Address]==112)
            {
                temp = digitalRead(A12);
            }
            else if(ByteArray[Address]==113)
            {
                temp = digitalRead(A13);
            }
            else if(ByteArray[Address]==114)
            {
                temp = digitalRead(A14);
            }
            else if(ByteArray[Address]==115)
            {
                temp = digitalRead(A15);
            }
            else
            {
              temp = digitalRead(ByteArray[Address]);              
            }            
            Serial.println((byte)temp);
          break;

          case X_Analog_Read:
            if (ByteArray[Address] == 0)
            {
                sensorValue = analogRead(A0);
           }
           else if (ByteArray[Address] == 1)
           {
               sensorValue = analogRead(A1);
           }
           else if (ByteArray[Address] == 2)
           {
               sensorValue = analogRead(A2);
           }
           else if (ByteArray[Address] == 3)
           {
               sensorValue = analogRead(A3);
           }
           else if (ByteArray[Address] == 4)
           {
               sensorValue = analogRead(A4);
           }
           else if (ByteArray[Address] == 5)
           {
               sensorValue = analogRead(A5);
           }
           else if (ByteArray[Address] == 6)
           {
               sensorValue = analogRead(A6);
           }
           else if (ByteArray[Address] == 7)
           {
               sensorValue = analogRead(A7);
           }
           else if (ByteArray[Address] == 8)
           {
               sensorValue = analogRead(A8);
           }
           else if (ByteArray[Address] == 9)
           {
               sensorValue = analogRead(A9);
           }
           else if (ByteArray[Address] == 10)
           {
               sensorValue = analogRead(A10);
           }
           else if (ByteArray[Address] == 11)
           {
               sensorValue = analogRead(A11);
           }
           else if (ByteArray[Address] == 12)
           {
               sensorValue = analogRead(A12);
           }
           else if (ByteArray[Address] == 13)
           {
               sensorValue = analogRead(A13);
           }
           else if (ByteArray[Address] == 14)
           {
               sensorValue = analogRead(A14);
           }
           else if (ByteArray[Address] == 15)
           {
               sensorValue = analogRead(A15);
           }           
            //voltage = sensorValue * (5.0 / 1023.0);
            Serial.println(sensorValue);
          break;  
          
          default:
          break;
        }        
           MsgComplete = false;
           ArrayIndex=0;
          delay(50);           
      }
  }
