using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;
using static System.Net.Mime.MediaTypeNames;
using System.IO.Ports;

namespace C_Sharp_Demo
{
    public partial class Form1 : Form
    {
        Timer timer = new Timer();
        int counter = 0;
        int time = 0;

        Dictionary<string, string> choiceToFile = new Dictionary<string, string>(){
            {"1", "1.txt"},
            {"2", "2.txt"}
        };
        bool active = false;

        [DllImport("BX_IV.dll")]
        public static extern int AddScreen(int nControlType, int nScreenNo,
        int nWidth, int nHeight, int nScreenType, int nPixelMode, int nDataDA,
           int nDataOE, int nRowOrder, int nFreqPar, string pCom, int nBaud,
           string pSocketIP, int nSocketPort, string pWiFiIP, int nWiFiPort, string pScreenStatusFile);

        [DllImport("BX_IV.dll")]
        public static extern int DeleteScreen(int nScreenNo);

        [DllImport("BX_IV.dll")]
        public static extern int SendScreenInfo(int nScreenNo, int nSendMode, int nSendCmd, int nOtherParam1);

        [DllImport("BX_IV.dll")]
        public static extern int AddScreenProgram(int nScreenNo, int nProgramType, int nPlayLength,
            int nStartYear, int nStartMonth, int nStartDay, int nEndYear, int nEndMonth, int nEndDay,
            int nMonPlay, int nTuesPlay, int nWedPlay, int nThursPlay, int bFriPlay, int nSatPlay, int nSunPlay,
            int nStartHour, int nStartMinute, int nEndHour, int nEndMinute); 

        [DllImport("BX_IV.dll")]
        public static extern int DeleteScreenProgram(int nScreenNo, int nProgramOrd);

        [DllImport("BX_IV.dll")]
        public static extern int DeleteScreenProgramArea(int nScreenNo, int nProgramOrd, int nAreaOrd);

        [DllImport("BX_IV.dll")]
        public static extern int AddScreenProgramBmpTextArea(int nScreenNo, int nProgramOrd, int nX, int nY,
            int nWidth, int nHeight);

        [DllImport("BX_IV.dll")]
        public static extern int AddScreenProgramAreaBmpTextFile(int nScreenNo, int nProgramOrd, int nAreaOrd,
            string pFileName, int nShowSingle, string pFontName, int nFontSize, int nBold, int nFontColor,
            int nStunt, int nRunSpeed, int nShowTime);

        [DllImport("BX_IV.dll")]
        public static extern int DeleteScreenProgramAreaBmpTextFile(int nScreenNo, int nProgramOrd, int nAreaOrd, int nFileOrd);

        [DllImport("BX_IV.dll")]
        public static extern int SetScreenAdjustLight(int nScreenNo, int nAdjustType,
            int nHandleLight, int nHour1, int nMinute1, int nLight1, int nHour2, int nMinute2,
            int nLight2, int nHour3, int nMinute3, int nLight3, int nHour4, int nMinute4, int nLight4);

        private const int RETURN_ERROR_AERETYPE = 0xF7;
        private const int RETURN_ERROR_RA_SCREENNO = 0xF8;
        private const int RETURN_ERROR_NOFIND_AREAFILE = 0xF9;
        private const int RETURN_ERROR_NOFIND_AREA = 0xFA;
        private const int RETURN_ERROR_NOFIND_PROGRAM = 0xFB;
        private const int RETURN_ERROR_NOFIND_SCREENNO = 0xFC;
        private const int RETURN_ERROR_NOW_SENDING = 0xFD;
        private const int RETURN_ERROR_OTHER = 0xFF;
        private const int RETURN_NOERROR = 0;

        private const int CONTROLLER_TYPE_3T = 0x10;
        private const int CONTROLLER_TYPE_3A = 0x20;
        private const int CONTROLLER_TYPE_3A1 = 0x21;
        private const int CONTROLLER_TYPE_3A2 = 0x22;
        private const int CONTROLLER_TYPE_3M = 0x30;

        private const int CONTROLLER_TYPE_4A1 = 0x0141;
        private const int CONTROLLER_TYPE_4A2 = 0x0241;
        private const int CONTROLLER_TYPE_4A3 = 0x0341;
        private const int CONTROLLER_TYPE_4AQ = 0x1041;
        private const int CONTROLLER_TYPE_4A = 0x0041;

        private const int CONTROLLER_TYPE_4M1 = 0x0142;
        private const int CONTROLLER_TYPE_4M = 0x0042;
        private const int CONTROLLER_TYPE_4MC = 0x0C42;
        private const int CONTROLLER_TYPE_4E = 0x0044;
        private const int CONTROLLER_TYPE_4C = 0x0043;
        private const int CONTROLLER_TYPE_4E1 = 0x0144;
        private const int CONTROLLER_TYPE_5M1 = 0x0052;
        private const int CONTROLLER_TYPE_5M4 = 0x0452;
        private const int CONTROLLER_TYPE_5E1 = 340;

        private const int SEND_MODE_COMM = 0;
        private const int SEND_MODE_NET = 2;

        private const int SEND_CMD_PARAMETER = 41471;
        private const int SEND_CMD_SENDALLPROGRAM = 41456;
        private const int SEND_CMD_POWERON = 41727;
        private const int SEND_CMD_POWEROFF = 41726;
        private const int SEND_CMD_TIMERPOWERONOFF = 41725;
        private const int SEND_CMD_CANCEL_TIMERPOWERONOFF = 41724;
        private const int SEND_CMD_RESIVETIME = 41723;
        private const int SEND_CMD_ADJUSTLIGHT = 41722;

        private const int SCREEN_NO = 1;
        private const int SCREEN_WIDTH = 64;
        private const int SCREEN_HEIGHT = 16;
        private const int SCREEN_TYPE = 1;
        private const int SCREEN_DATADA = 0;
        private const int SCREEN_DATAOE = 0;
        private const string SCREEN_COMM = "COM1";
        private const int SCREEN_BAUD = 57600;
        private const int SCREEN_ROWORDER = 0;
        private const int SCREEN_FREQPAR = 0;
        private const string SCREEN_SOCKETIP = "192.168.0.100";
        private const int SCREEN_SOCKETPORT = 5005;
        private const string SCREEN_WIFIIP = "192.168.0.100";
        private const int SCREEN_WIFIPORT = 5005;
        private bool m_bSendBusy = false;


        private SerialPort portArduino = new SerialPort("COM4", 9600);
        public Form1()
        {
            InitializeComponent();
            tabControl1.SelectedIndexChanged += tabIndexChanged;
        }

        public void GetErrorMessage(string szfunctionName, int nResult)
        {
            string szResult;
            DateTime dt = DateTime.Now;
            szResult = dt.ToString() + "--- Выполнение функций：" + szfunctionName + "--- Возврат результатов：";
            switch (nResult)
            {
                case RETURN_ERROR_AERETYPE:
                    rchMessage.Text += szResult + "Ошибка типа области\r\n";
                    break;
                case RETURN_ERROR_RA_SCREENNO:
                    rchMessage.Text += szResult + "Информация об этом дисплее уже доступна.\r\n";
                    break;
                case RETURN_ERROR_NOFIND_AREAFILE:
                    rchMessage.Text += szResult + "Не найден файл области\r\n";
                    break;
                case RETURN_ERROR_NOFIND_AREA:
                    rchMessage.Text += szResult + "Не найдена область для вывода\r\n";
                    break;
                case RETURN_ERROR_NOFIND_PROGRAM:
                    rchMessage.Text += szResult + "Програма для выполнения не найдена.\r\n";
                    break;
                case RETURN_ERROR_NOFIND_SCREENNO:
                    rchMessage.Text += szResult + "Дисплей не найден\r\n";
                    break;
                case RETURN_ERROR_NOW_SENDING:
                    rchMessage.Text += szResult + "Связь устанавливается, пожалуйста, подождите\r\n";
                    break;
                case RETURN_ERROR_OTHER:
                    rchMessage.Text += szResult + "Другие ошибки\r\n";
                    break;
                case RETURN_NOERROR:
                    rchMessage.Text += szResult + "Успешно\r\n";
                    break;
                case 0x01:
                    rchMessage.Text += szResult + "Ошибка связи\r\n";
                    break;
                case 0x02:
                    rchMessage.Text += szResult + "Ошибка связи\r\n";
                    break;
                case 0x03:
                    rchMessage.Text += szResult + "Ошибка связи\r\n";
                    break;
                case 0x04:
                    rchMessage.Text += szResult + "Ошибка связи\r\n";
                    break;
                case 0x05:
                    rchMessage.Text += szResult + "Ошибка связи\r\n";
                    break;
                case 0x06:
                    rchMessage.Text += szResult + "Ошибка связи\r\n";
                    break;
                case 0x07:
                    rchMessage.Text += szResult + "Ошибка связи\r\n";
                    break;
                case 0x08:
                    rchMessage.Text += szResult + "Ошибка связи\r\n";
                    break;
                case 0x09:
                    rchMessage.Text += szResult + "Ошибка связи\r\n";
                    break;
                case 0x0A:
                    rchMessage.Text += szResult + "Ошибка связи\r\n";
                    break;
                case 0x0B:
                    rchMessage.Text += szResult + "Ошибка связи\r\n";
                    break;
                case 0x0C:
                    rchMessage.Text += szResult + "Ошибка связи\r\n";
                    break;
                case 0x0D:
                    rchMessage.Text += szResult + "Ошибка связи\r\n";
                    break;
                case 0x0E:
                    rchMessage.Text += szResult + "Ошибка связи\r\n";
                    break;
                case 0x0F:
                    rchMessage.Text += szResult + "Ошибка связи\r\n";
                    break;
                case 0x10:
                    rchMessage.Text += szResult + "Ошибка связи\r\n";
                    break;
                case 0x11:
                    rchMessage.Text += szResult + "Ошибка связи\r\n";
                    break;
                case 0x12:
                    rchMessage.Text += szResult + "Ошибка связи\r\n";
                    break;
                case 0x13:
                    rchMessage.Text += szResult + "Ошибка связи\r\n";
                    break;
                case 0x14:
                    rchMessage.Text += szResult + "Ошибка связи\r\n";
                    break;
                case 0x15:
                    rchMessage.Text += szResult + "Ошибка связи\r\n";
                    break;
                case 0x16:
                    rchMessage.Text += szResult + "Ошибка связи\r\n";
                    break;
                case 0x17:
                    rchMessage.Text += szResult + "Ошибка связи\r\n";
                    break;
                case 0x18:
                    rchMessage.Text += szResult + "Ошибка связи\r\n";
                    break;
                case 0xFE:
                    rchMessage.Text += szResult + "ошибка связи\r\n";
                    break;
                case 123123:
                    rchMessage.Text += szResult + "idk\r\n";
                    break;
            }

        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (m_bSendBusy == false && !timer.Enabled)
            {
                m_bSendBusy = true;
                int result;
                if (active)
                {
                    result = DeleteScreenProgram(SCREEN_NO, 0);
                    GetErrorMessage("DeleteScreenProgram", result);
                    GetErrorMessage("AddScreen", result);
                    result = AddScreenProgram(SCREEN_NO, 0, 0, 65535, 12, 3, 2011, 11, 26, 1, 1, 1, 1, 1, 1, 1, 0, 0, 23, 59);
                }
                else
                {
                    result = AddScreen(CONTROLLER_TYPE_5M1, SCREEN_NO, SCREEN_WIDTH, SCREEN_HEIGHT, SCREEN_TYPE, 1,
                    SCREEN_DATADA, SCREEN_DATAOE, SCREEN_ROWORDER, SCREEN_FREQPAR, SCREEN_COMM, SCREEN_BAUD, SCREEN_SOCKETIP, SCREEN_SOCKETPORT,
                    SCREEN_WIFIIP, SCREEN_WIFIPORT, "C:\\ScreenStatus.ini");
                    active = true;
                    result = AddScreenProgram(SCREEN_NO, 0, 0, 65535, 12, 3, 2011, 11, 26, 1, 1, 1, 1, 1, 1, 1, 0, 0, 23, 59);
                    GetErrorMessage("AddScreenProgram", result);
                }
                result = SetScreenAdjustLight(SCREEN_NO, 0, Convert.ToInt32(numericUpDown3.Value), 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 ,0);
                GetErrorMessage("SetScreenAdjustLight", result);
                result = SendScreenInfo(SCREEN_NO, SEND_MODE_NET, SEND_CMD_ADJUSTLIGHT, 0);
                GetErrorMessage("SendScreenInfo", result);
                result = AddScreenProgramBmpTextArea(SCREEN_NO, 0, 0, 0, SCREEN_WIDTH, SCREEN_HEIGHT);
                GetErrorMessage("AddScreenProgramBmpTextArea", result);
                //string fileName;
                //choiceToFile.TryGetValue(comboBox1.Text, out fileName);
                string fileName = "edit.txt";
                StreamWriter sw = new StreamWriter(fileName);
                string text = textBox1.Text;
                sw.WriteLine(text);
                sw.Close();
                int stat = checkBox2.Checked ? 1 : 4;
                result = AddScreenProgramAreaBmpTextFile(SCREEN_NO, 0, 0, fileName, 1, "Tahoma", Convert.ToInt32(numericUpDown1.Value), 
                    Convert.ToInt32(checkBox1.Checked), 65535, stat, 3, 0);
                GetErrorMessage("AddScreenProgramAreaBmpTextFile", result);
                int nResult;
                nResult = SendScreenInfo(SCREEN_NO, SEND_MODE_NET, SEND_CMD_SENDALLPROGRAM, 0);
                GetErrorMessage("SendScreenInfo", nResult);
                textBox2.Text = text;
                m_bSendBusy = false;
            }
        }
        private void SetOnbonTextFromTxt()
        {
            if (m_bSendBusy == false)
            {
                m_bSendBusy = true;
                int result;
                if (active)
                {
                    result = DeleteScreenProgram(SCREEN_NO, 0);
                    GetErrorMessage("DeleteScreenProgram", result);
                    GetErrorMessage("AddScreen", result);
                    result = AddScreenProgram(SCREEN_NO, 0, 0, 65535, 12, 3, 2011, 11, 26, 1, 1, 1, 1, 1, 1, 1, 0, 0, 23, 59);
                }
                else
                {
                    result = AddScreen(CONTROLLER_TYPE_5M1, SCREEN_NO, SCREEN_WIDTH, SCREEN_HEIGHT, SCREEN_TYPE, 1,
                    SCREEN_DATADA, SCREEN_DATAOE, SCREEN_ROWORDER, SCREEN_FREQPAR, SCREEN_COMM, SCREEN_BAUD, SCREEN_SOCKETIP, SCREEN_SOCKETPORT,
                    SCREEN_WIFIIP, SCREEN_WIFIPORT, "C:\\ScreenStatus.ini");
                    active = true;
                    result = AddScreenProgram(SCREEN_NO, 0, 0, 65535, 12, 3, 2011, 11, 26, 1, 1, 1, 1, 1, 1, 1, 0, 0, 23, 59);
                    GetErrorMessage("AddScreenProgram", result);
                }
                result = SetScreenAdjustLight(SCREEN_NO, 0, Convert.ToInt32(numericUpDown3.Value), 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
                GetErrorMessage("SetScreenAdjustLight", result);
                result = SendScreenInfo(SCREEN_NO, SEND_MODE_NET, SEND_CMD_ADJUSTLIGHT, 0);
                GetErrorMessage("SendScreenInfo", result);
                result = AddScreenProgramBmpTextArea(SCREEN_NO, 0, 0, 0, SCREEN_WIDTH, SCREEN_HEIGHT);
                GetErrorMessage("AddScreenProgramBmpTextArea", result);
                string text = "";
                string[] line = { };
                try
                {
                    line = File.ReadAllLines("text.txt");
                }
                catch (Exception e)
                {
                    GetErrorMessage(e.Message, 123123);
                }
                if (line.Length == 0)
                {
                    return;
                }
                if (counter >= line.Length)
                {
                    counter = 0;
                }
                text = line[counter];
                counter++;
                string fileName = "edit.txt";
                StreamWriter sw = new StreamWriter(fileName);
                sw.WriteLine(text);
                sw.Close();
                int stat = checkBox2.Checked ? 1 : 4;
                result = AddScreenProgramAreaBmpTextFile(SCREEN_NO, 0, 0, fileName, 1, "Tahoma", Convert.ToInt32(numericUpDown1.Value),
                    Convert.ToInt32(checkBox1.Checked), 65535, stat, 3, 0);
                GetErrorMessage("AddScreenProgramAreaBmpTextFile", result);
                int nResult;
                nResult = SendScreenInfo(SCREEN_NO, SEND_MODE_NET, SEND_CMD_SENDALLPROGRAM, 0);
                GetErrorMessage("SendScreenInfo", nResult);
                textBox2.Text = text;
                m_bSendBusy = false;
            }
        }

        private void SetArduinoTextFromTxt()
        {
            string text = "";
            string[] line = { };
            try
            {
                line = File.ReadAllLines("text.txt");
            }
            catch(Exception ex)
            {
                MessageBox.Show("Something went wrong with text.txt");
            }
            if (line.Length == 0)
            {
                return;
            }
            if (counter >= line.Length)
            {
                counter = 0;
            }
            text = line[counter];
            counter++;
            byte[] message = System.Text.Encoding.UTF8.GetBytes(text);
            portArduino.Write(message, 0, message.Length);
            textBox4.Text = text;
        }

        private void OnbonTimerEvent(Object myObject, EventArgs myEventArgs)
        {
            timer.Stop();
            SetOnbonTextFromTxt();
            timer.Enabled = true;
        }

        private void ArduinoTimerEvent(Object myObject, EventArgs myEventArgs)
        {
            timer.Stop();
            SetArduinoTextFromTxt();
            timer.Enabled = true;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        private void button13_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.Application.Exit();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (timer.Enabled)
            {
                timer.Stop();
                timer.Tick -= OnbonTimerEvent;
                button2.Text = "Запустить таймер";
            }
            else
            {
                counter = 0;
                SetOnbonTextFromTxt();
                time = Convert.ToInt32(numericUpDown2.Value);
                timer.Tick += OnbonTimerEvent;
                timer.Interval = time;
                button2.Text = "Остановить таймер";
                timer.Start();
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            string inputText = textBox3.Text;
            byte[] message = System.Text.Encoding.UTF8.GetBytes(inputText);
            byte[] brightness = System.Text.Encoding.UTF8.GetBytes(numericUpDown4.Value.ToString());
            byte[] staticDisp = System.Text.Encoding.UTF8.GetBytes(checkBox4.Checked ? "1" : "0");
            portArduino.Write(brightness, 0, brightness.Length);
            portArduino.Write(message, 0, message.Length);
            portArduino.Write(staticDisp, 0, staticDisp.Length);
        }

        private void tabIndexChanged(object sender, EventArgs e)
        {
            if (tabControl1.SelectedTab == tabControl1.TabPages["tabPage1"])
            {
                portArduino.Close();
                
            }
            else if(tabControl1.SelectedTab == tabControl1.TabPages["tabPage2"])
            {
                try {
                    portArduino.Open();
                }
                catch
                {
                    MessageBox.Show("No device on COM4");
                }
            }
            else if(tabControl1.SelectedTab == tabControl1.TabPages["tabPage3"])
            {
                portArduino.Close();
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (timer.Enabled)
            {
                timer.Stop();
                timer.Tick -= ArduinoTimerEvent;
                button4.Text = "Запустить таймер";
            }
            else
            {
                counter = 0;
                SetArduinoTextFromTxt();
                time = Convert.ToInt32(numericUpDown4.Value);
                timer.Tick += ArduinoTimerEvent;
                timer.Interval = time;
                button4.Text = "Остановить таймер";
                timer.Start();
            }
        }
    }
}

