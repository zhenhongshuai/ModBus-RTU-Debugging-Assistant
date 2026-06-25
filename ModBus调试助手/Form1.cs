using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ModBus调试助手
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        byte DeviceID = 0x01;  //设备地址
        byte[] Senddata = { 0x01, 0x04, 0x00, 0x02, 0x00, 0x01, 0x90, 0x0A };
        UInt16 DeviceSearchID = 0x01;
        int DeviceCount = 0;
        byte CMD = 0x00;

        private void Form1_Load(object sender, EventArgs e)
        {
            string[] ports = System.IO.Ports.SerialPort.GetPortNames();
            comboBox1.Items.AddRange(ports);
            if (comboBox1.Items.Count > 0)
            {
                comboBox1.SelectedIndex = 0;
                serialPort1.PortName = comboBox1.Text;
            }
            comboBox2.SelectedIndex = 3;
            textBox1.Text = DeviceID.ToString("X2");
            //progressBar1.Visible = false;
        }

        private void butCOM_Click(object sender, EventArgs e)
        {
            if (serialPort1.IsOpen == false)
            {
                try
                {
                    serialPort1.Open();
                    butCOM.Text = "关闭串口";
                    comboBox1.Enabled = false;
                }
                catch (Exception)
                {
                    MessageBox.Show("串口已打开");
                }

            }
            else
            {
                serialPort1.Close();
                butCOM.Text = "打开串口";
                comboBox1.Enabled = true;
                if (timer1.Enabled == true)
                {
                    timer1.Enabled = false;
                    butSearch.Text = "搜索设备";
                    progressBar1.Visible = false;
                }
            }
        }

        private void butSearch_Click(object sender, EventArgs e)
        {
            if (serialPort1.IsOpen == false)
            {
                MessageBox.Show("串口未打开！");
                return;
            }
            if (timer1.Enabled == false)
            {
                timer1.Enabled = true;
                butSearch.Text = "停止搜索";
                listBox1.Items.Clear();
                DeviceSearchID = 0x01;
                DeviceCount = 0;
                label8.Text = "0";
                serialPort1.DiscardInBuffer();
                progressBar1.Visible = true;
            }
            else
            {
                timer1.Enabled = false;
                butSearch.Text = "搜索设备";
                DeviceSearchID = 0x01;
                label3.Text = "操作停止";
                label8.Text = DeviceCount.ToString();
                progressBar1.Visible = false;
                if (listBox1.Items.Count == 1)
                {
                    DeviceID = Convert.ToByte(listBox1.Items[0].ToString(), 16);
                    Senddata[0] = DeviceID;
                    label5.Text = DeviceID.ToString();
                }
            }
        }

        private void butSETID_Click(object sender, EventArgs e)
        {
            if (serialPort1.IsOpen == false)
            {
                MessageBox.Show("串口未打开！");
                return;
            }
            UInt16 crcval;
            if (textBox1.Text.Length <= 2)
            {
                DeviceID = Convert.ToByte(textBox1.Text, 16);
                Senddata[1] = 0x06;
                Senddata[3] = 0x02;
                Senddata[5] = DeviceID;
                crcval = GetCheckCode(Senddata, 6);
                Senddata[7] = (byte)(crcval >> 8);
                Senddata[6] = (byte)(crcval);
                label3.Text = "已发送";
                CMD = 0x00;
                serialPort1.DiscardInBuffer();
                serialPort1.Write(Senddata, 0, 8);
            }
            else
            {
                MessageBox.Show("地址数据过长！");
            }
        }

        private UInt16 GetCheckCode(byte[] pSendBuf, byte nEnd)//获得CRC校验码 
        {
            UInt16 i, j;
            UInt16 wCrc = 0xFFFF;
            for (i = 0; i < nEnd; i++)
            {
                wCrc ^= (UInt16)pSendBuf[i];
                for (j = 0; j < 8; j++)
                {
                    if ((wCrc & 1) == 1)
                    {
                        wCrc >>= 1;
                        wCrc ^= 0xA001;
                    }
                    else
                    {
                        wCrc >>= 1;
                    }
                }
            }
            return wCrc;
        }
        // CRC16/ModBus 计算
        private ushort CalculateCrc16Modbus(byte[] data, int length)
        {
            ushort crc = 0xFFFF;
            for (int i = 0; i < length; i++)
            {
                crc ^= data[i];
                for (int j = 0; j < 8; j++)
                {
                    if ((crc & 0x0001) == 1)
                    {
                        crc >>= 1;
                        crc ^= 0xA001;
                    }
                    else
                    {
                        crc >>= 1;
                    }
                }
            }
            return crc;
        }

        // CRC16/ModBus 计算重载
        private ushort CalculateCrc16Modbus(byte[] data)
        {
            return CalculateCrc16Modbus(data, data.Length);
        }

        // 将十六进制字符串转为字节数组
        private byte[] HexStringToByteArray(string hex)
        {
            // 去除可能存在的空格
            hex = hex.Replace(" ", "");
            int len = hex.Length;
            if (len % 2 != 0)
                throw new ArgumentException("十六进制字符串长度必须为偶数！");

            byte[] bytes = new byte[len / 2];
            for (int i = 0; i < len; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return bytes;
        }

        private void serialPort1_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            this.Invoke(new EventHandler(rcedata));
        }
        /// <summary>
        /// 串口接收
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rcedata(object sender, EventArgs e)
        {
            try
            {
                int n = serialPort1.BytesToRead;
                if (n > 6)
                {
                    byte[] buf = new byte[n];
                    serialPort1.Read(buf, 0, n);

                    // ===== 显示接收到的原始数据（16进制）=====
                    string time = DateTime.Now.ToString("HH:mm:ss.fff");
                    string hexDisplay = BitConverter.ToString(buf).Replace("-", " ");
                    AppendToLog(time + " [接收] " + hexDisplay);

                    if (buf[1] == 0x06 && CMD == 0x00)
                    {
                        label3.Text = "设置成功!";
                        label5.Text = DeviceID.ToString("X2");
                        serialPort1.DiscardInBuffer();
                        AppendToLog(time + " [事件] 设置成功 (CMD=0x00)");
                    }

                    if (buf[1] == 0x06 && CMD == 0x01)
                    {
                        label3.Text = "设置成功!";
                        string bdr = buf[5].ToString("X2");
                        if (bdr == "00")
                        {
                            label13.Text = "1200";
                            serialPort1.BaudRate = 1200;
                        }
                        if (bdr == "01")
                        {
                            label13.Text = "2400";
                            serialPort1.BaudRate = 2400;
                        }
                        if (bdr == "02")
                        {
                            label13.Text = "4800";
                            serialPort1.BaudRate = 4800;
                        }
                        if (bdr == "03")
                        {
                            label13.Text = "9600";
                            serialPort1.BaudRate = 9600;
                        }
                        if (bdr == "04")
                        {
                            label13.Text = "19200";
                            serialPort1.BaudRate = 19200;
                        }
                        serialPort1.DiscardInBuffer();
                        AppendToLog(time + " [事件] 设置成功 (CMD=0x01) 波特率=" + label13.Text);
                    }

                    if (buf[1] == 0x04 && checkBox1.Checked == true)
                    {
                        //AppendToLog(time + " [事件] 处理ModBus请求 (功能码0x04)");
                        ProcessModBusRequest(buf);
                    }
                }
            }
            catch (Exception ex)
            {
                string time = DateTime.Now.ToString("HH:mm:ss.fff");
                AppendToLog(time + " [错误] 接收处理失败: " + ex.Message);
            }
        }

        // ===== 日志添加方法 =====
        private void AppendToLog(string message)
        {
            // 确保在UI线程执行
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<string>(AppendToLog), message);
                return;
            }

            txtReceiveBox1.AppendText(message + Environment.NewLine);

            // 限制最大行数（例如1000行），防止内存溢出
            if (txtReceiveBox1.Lines.Length > 1000)
            {
                string[] lines = txtReceiveBox1.Lines;
                string[] newLines = new string[500];
                Array.Copy(lines, lines.Length - 500, newLines, 0, 500);
                txtReceiveBox1.Lines = newLines;
            }

            // 自动滚动到最后一行
            txtReceiveBox1.ScrollToCaret();
        }
        // 主机发送数据处理并返回应答
        private byte[] ProcessModBusRequest(byte[] requestData)
        {
            try
            {
                // 1. 解析主机请求
                byte deviceAddr = requestData[0];        // 从站地址
                byte funcCode = requestData[1];          // 功能码
                ushort startAddr = (ushort)((requestData[2] << 8) | requestData[3]);  // 起始地址
                ushort quantity = (ushort)((requestData[4] << 8) | requestData[5]);   // 寄存器数量

                // 2. 判断读取几个寄存器
                byte[] responseData = null;

                if (quantity == 1)
                {
                    // 读取1个寄存器：使用 textBox8 的值
                    string regValue = textBox8.Text.Trim();
                    ushort value = Convert.ToUInt16(regValue, 16);

                    // 构建应答：地址 + 功能码 + 字节数(2) + 寄存器值(2字节) + CRC
                    responseData = BuildResponse(deviceAddr, funcCode, new ushort[] { value });

                    //MessageBox.Show($"应答1个寄存器：{regValue} (0x{value:X4})");
                }
                else if (quantity == 2)
                {
                    // 读取2个寄存器：使用 textBox8 和 textBox9 的值
                    string regValue1 = textBox8.Text.Trim();
                    string regValue2 = textBox9.Text.Trim();

                    ushort value1 = Convert.ToUInt16(regValue1, 16);
                    ushort value2 = Convert.ToUInt16(regValue2, 16);

                    // 构建应答：地址 + 功能码 + 字节数(4) + 寄存器值(4字节) + CRC
                    responseData = BuildResponse(deviceAddr, funcCode, new ushort[] { value1, value2 });

                    serialPort1.Write(responseData, 0, responseData.Length);
                }
                else if (quantity == 3)
                {
                    // 读取2个寄存器：使用 textBox8 和 textBox9 的值
                    string regValue1 = textBox8.Text.Trim();
                    string regValue2 = textBox9.Text.Trim();
                    string regValue3 = textBox10.Text.Trim();

                    ushort value1 = Convert.ToUInt16(regValue1, 16);
                    ushort value2 = Convert.ToUInt16(regValue2, 16);
                    ushort value3 = Convert.ToUInt16(regValue3, 16);

                    // 构建应答：地址 + 功能码 + 字节数(4) + 寄存器值(4字节) + CRC
                    responseData = BuildResponse(deviceAddr, funcCode, new ushort[] { value1, value2, value3 });

                    serialPort1.Write(responseData, 0, responseData.Length);
                }
                else
                {
                    MessageBox.Show("不支持的数量：" + quantity, "错误");
                    return null;
                }

                return responseData;
            }
            catch (Exception ex)
            {
                MessageBox.Show("处理请求失败：" + ex.Message, "错误");
                return null;
            }
        }

        // 构建 ModBus 应答数据包
        private byte[] BuildResponse(byte deviceAddr, byte funcCode, ushort[] values)
        {
            // 应答格式：地址 + 功能码 + 字节数 + 寄存器值(每个2字节) + CRC
            int byteCount = values.Length * 2;  // 每个寄存器占2个字节
            byte[] response = new byte[3 + byteCount + 2];  // 3 + 数据长度 + CRC

            int index = 0;
            response[index++] = deviceAddr;      // 从站地址
            response[index++] = funcCode;        // 功能码
            response[index++] = (byte)byteCount; // 字节数

            // 添加寄存器值（高字节在前）
            for (int i = 0; i < values.Length; i++)
            {
                response[index++] = (byte)(values[i] >> 8);   // 高字节
                response[index++] = (byte)(values[i] & 0xFF); // 低字节
            }

            // 计算CRC（不包含CRC本身）
            ushort crc = CalculateCrc16Modbus(response, index);
            response[index++] = (byte)(crc & 0xFF);      // CRC低字节
            response[index] = (byte)((crc >> 8) & 0xFF); // CRC高字节

            return response;
        }
        private void butJS_Click(object sender, EventArgs e)
        {
            try
            {
                // 1. 获取各文本框内容（视为十六进制字符串）
                string deviceAddr = textBox2.Text.Trim();  // "02"
                string funcCode = textBox3.Text.Trim();    // "04"
                string regAddr = textBox4.Text.Trim();     // "0000"
                string dataLen = textBox5.Text.Trim();     // "0002"

                // 2. 拼接成完整的十六进制字符串（无分隔符）
                string combinedHex = deviceAddr + funcCode + regAddr + dataLen;
                // 结果： "020400000002"

                // 3. 将十六进制字符串解析为字节数组
                byte[] dataBytes = HexStringToByteArray(combinedHex);
                // 结果： [0x02, 0x04, 0x00, 0x00, 0x00, 0x02]

                // 4. 计算CRC-16/Modbus
                ushort crcResult = CalculateCrc16Modbus(dataBytes);
                // 预期结果： 0xF871（低字节0x71，高字节0xF8）

                // 5. 构建完整数据包（字节数组）
                // 先将原始数据转为字节列表
                List<byte> fullBytes = new List<byte>(dataBytes);
                // 添加CRC（低字节在前）
                fullBytes.Add((byte)(crcResult & 0xFF));      // 0x71
                fullBytes.Add((byte)((crcResult >> 8) & 0xFF)); // 0xF8
                byte[] fullData = fullBytes.ToArray();

                // 6. textBox6：每两个字符加一个空格（如 "02 04 00 00 00 02 71 F8"）
                string hexWithSpace = BitConverter.ToString(fullData).Replace("-", " ");
                textBox6.Text = hexWithSpace;

                // 7. textBox7：C语言数组格式（如 "0x02, 0x04, 0x00, 0x00, 0x00, 0x02, 0x71, 0xF8"）
                string cArrayFormat = string.Join(", ", fullData.Select(b => "0x" + b.ToString("X2")));
                textBox7.Text = cArrayFormat;
            }
            catch (Exception ex)
            {
                MessageBox.Show("请输入有效的十六进制字符（0-9, A-F）！\n" + ex.Message);
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void butBaudrate_Click(object sender, EventArgs e)
        {
            if (serialPort1.IsOpen == false)
            {
                MessageBox.Show("串口未打开！");
                return;
            }
            UInt16 crcval;
            Senddata[0] = DeviceID;
            Senddata[1] = 0x06;
            Senddata[3] = 0x04;
            Senddata[5] = Convert.ToByte(comboBox2.Text.Substring(0, 2), 16);
            crcval = GetCheckCode(Senddata, 6);
            Senddata[7] = (byte)(crcval >> 8);
            Senddata[6] = (byte)(crcval);
            label3.Text = "已发送";
            CMD = 0x01;
            serialPort1.DiscardInBuffer();
            serialPort1.Write(Senddata, 0, 8);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            UInt16 crcval;
            Senddata[0] = (byte)DeviceSearchID;
            Senddata[1] = 0x04;
            Senddata[3] = 0x03;
            Senddata[5] = 0x01;
            crcval = GetCheckCode(Senddata, 6);
            Senddata[7] = (byte)(crcval >> 8);
            Senddata[6] = (byte)(crcval);
            label3.Text = "已发送";
            CMD = 0x03;
            serialPort1.Write(Senddata, 0, 8);
            DeviceSearchID++;
            progressBar1.Value = DeviceSearchID;
            if (DeviceSearchID > 0xFF)
            {
                DeviceSearchID = 0x01;
                timer1.Enabled = false;
                progressBar1.Visible = false;
                label3.Text = "搜索完成";
                label8.Text = DeviceCount.ToString();
                butSearch.Text = "搜索设备";
                if (listBox1.Items.Count == 1)
                {
                    DeviceID = Convert.ToByte(listBox1.Items[0].ToString(), 16);
                    Senddata[0] = DeviceID;
                    label5.Text = DeviceID.ToString();
                }
            }
        }

        private void txtBoxSend1_DoubleClick_1(object sender, EventArgs e)
        {
            string input = MyInputBox.Show(
                "请输入新的内容：",    // prompt：提示信息
                "修改文本",            // title：标题
                ""          // defaultValue：默认值（当前文本框的内容）
            );

            // 如果用户点击了确定（输入内容不为空），更新文本框
            if (!string.IsNullOrEmpty(input))
            {
                butSend1.Text = input;
            }
        }

        private void txtBoxSend2_DoubleClick(object sender, EventArgs e)
        {
            string input = MyInputBox.Show(
               "请输入新的内容：",    // prompt：提示信息
               "修改文本",            // title：标题
               ""          // defaultValue：默认值（当前文本框的内容）
           );

            // 如果用户点击了确定（输入内容不为空），更新文本框
            if (!string.IsNullOrEmpty(input))
            {
                butSend2.Text = input;
            }
        }

        private void txtBoxSend3_DoubleClick(object sender, EventArgs e)
        {
            string input = MyInputBox.Show(
               "请输入新的内容：",    // prompt：提示信息
               "修改文本",            // title：标题
               ""          // defaultValue：默认值（当前文本框的内容）
           );

            // 如果用户点击了确定（输入内容不为空），更新文本框
            if (!string.IsNullOrEmpty(input))
            {
                butSend3.Text = input;
            }
        }

        private void txtBoxSend4_DoubleClick(object sender, EventArgs e)
        {
            string input = MyInputBox.Show(
               "请输入新的内容：",    // prompt：提示信息
               "修改文本",            // title：标题
               ""          // defaultValue：默认值（当前文本框的内容）
           );

            // 如果用户点击了确定（输入内容不为空），更新文本框
            if (!string.IsNullOrEmpty(input))
            {
                butSend4.Text = input;
            }
        }

        private void txtBoxSend5_DoubleClick(object sender, EventArgs e)
        {
            string input = MyInputBox.Show(
               "请输入新的内容：",    // prompt：提示信息
               "修改文本",            // title：标题
               ""          // defaultValue：默认值（当前文本框的内容）
           );

            // 如果用户点击了确定（输入内容不为空），更新文本框
            if (!string.IsNullOrEmpty(input))
            {
                butSend5.Text = input;
            }
        }

        private void txtBoxSend6_DoubleClick(object sender, EventArgs e)
        {
            string input = MyInputBox.Show(
               "请输入新的内容：",    // prompt：提示信息
               "修改文本",            // title：标题
               ""          // defaultValue：默认值（当前文本框的内容）
           );

            // 如果用户点击了确定（输入内容不为空），更新文本框
            if (!string.IsNullOrEmpty(input))
            {
                butSend6.Text = input;
            }
        }

        private void txtBoxSend7_DoubleClick(object sender, EventArgs e)
        {
            string input = MyInputBox.Show(
               "请输入新的内容：",    // prompt：提示信息
               "修改文本",            // title：标题
               ""          // defaultValue：默认值（当前文本框的内容）
           );

            // 如果用户点击了确定（输入内容不为空），更新文本框
            if (!string.IsNullOrEmpty(input))
            {
                butSend7.Text = input;
            }
        }

        private void txtBoxSend8_DoubleClick(object sender, EventArgs e)
        {
            string input = MyInputBox.Show(
               "请输入新的内容：",    // prompt：提示信息
               "修改文本",            // title：标题
               ""          // defaultValue：默认值（当前文本框的内容）
           );

            // 如果用户点击了确定（输入内容不为空），更新文本框
            if (!string.IsNullOrEmpty(input))
            {
                butSend8.Text = input;
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            serialPort1.PortName = comboBox1.Text;
        }

        private void butSend1_Click(object sender, EventArgs e)
        {
            try
            {
                // 检查串口是否打开
                if (!serialPort1.IsOpen)
                {
                    MessageBox.Show("请先打开串口！", "提示");
                    return;
                }

                // 获取要发送的文本
                string sendText = txtBoxSend1.Text.Trim();
                if (string.IsNullOrEmpty(sendText))
                {
                    MessageBox.Show("请输入要发送的数据！", "提示");
                    return;
                }

                // 判断是否勾选16进制发送
                if (checkBoxHex1.Checked)
                {
                    // ===== 发送16进制数据 =====
                    // 去掉空格，按每两个字符转换
                    string hex = sendText.Replace(" ", "");
                    if (hex.Length % 2 != 0)
                    {
                        MessageBox.Show("16进制数据长度必须为偶数！", "错误");
                        return;
                    }

                    // 转换为字节数组
                    byte[] data = new byte[hex.Length / 2];
                    for (int i = 0; i < hex.Length; i += 2)
                    {
                        data[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
                    }

                    // 发送16进制数据
                    serialPort1.Write(data, 0, data.Length);
                }
                else
                {
                    // ===== 发送字符串数据 =====
                    byte[] data = System.Text.Encoding.ASCII.GetBytes(sendText);
                    serialPort1.Write(data, 0, data.Length);
                }
            }
            catch (FormatException)
            {
                MessageBox.Show("16进制数据格式错误！请检查是否包含非法字符。", "错误");
            }
            catch (Exception ex)
            {
                MessageBox.Show("发送失败：" + ex.Message, "错误");
            }
        }

        private void butSend2_Click(object sender, EventArgs e)
        {
            try
            {
                // 检查串口是否打开
                if (!serialPort1.IsOpen)
                {
                    MessageBox.Show("请先打开串口！", "提示");
                    return;
                }

                // 获取要发送的文本
                string sendText = txtBoxSend2.Text.Trim();
                if (string.IsNullOrEmpty(sendText))
                {
                    MessageBox.Show("请输入要发送的数据！", "提示");
                    return;
                }

                // 判断是否勾选16进制发送
                if (checkBoxHex2.Checked)
                {
                    // ===== 发送16进制数据 =====
                    // 去掉空格，按每两个字符转换
                    string hex = sendText.Replace(" ", "");
                    if (hex.Length % 2 != 0)
                    {
                        MessageBox.Show("16进制数据长度必须为偶数！", "错误");
                        return;
                    }

                    // 转换为字节数组
                    byte[] data = new byte[hex.Length / 2];
                    for (int i = 0; i < hex.Length; i += 2)
                    {
                        data[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
                    }

                    // 发送16进制数据
                    serialPort1.Write(data, 0, data.Length);
                }
                else
                {
                    // ===== 发送字符串数据 =====
                    byte[] data = System.Text.Encoding.ASCII.GetBytes(sendText);
                    serialPort1.Write(data, 0, data.Length);
                }
            }
            catch (FormatException)
            {
                MessageBox.Show("16进制数据格式错误！请检查是否包含非法字符。", "错误");
            }
            catch (Exception ex)
            {
                MessageBox.Show("发送失败：" + ex.Message, "错误");
            }
        }

        private void butSend3_Click(object sender, EventArgs e)
        {
            try
            {
                // 检查串口是否打开
                if (!serialPort1.IsOpen)
                {
                    MessageBox.Show("请先打开串口！", "提示");
                    return;
                }

                // 获取要发送的文本
                string sendText = txtBoxSend3.Text.Trim();
                if (string.IsNullOrEmpty(sendText))
                {
                    MessageBox.Show("请输入要发送的数据！", "提示");
                    return;
                }

                // 判断是否勾选16进制发送
                if (checkBoxHex3.Checked)
                {
                    // ===== 发送16进制数据 =====
                    // 去掉空格，按每两个字符转换
                    string hex = sendText.Replace(" ", "");
                    if (hex.Length % 2 != 0)
                    {
                        MessageBox.Show("16进制数据长度必须为偶数！", "错误");
                        return;
                    }

                    // 转换为字节数组
                    byte[] data = new byte[hex.Length / 2];
                    for (int i = 0; i < hex.Length; i += 2)
                    {
                        data[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
                    }

                    // 发送16进制数据
                    serialPort1.Write(data, 0, data.Length);
                }
                else
                {
                    // ===== 发送字符串数据 =====
                    byte[] data = System.Text.Encoding.ASCII.GetBytes(sendText);
                    serialPort1.Write(data, 0, data.Length);
                }
            }
            catch (FormatException)
            {
                MessageBox.Show("16进制数据格式错误！请检查是否包含非法字符。", "错误");
            }
            catch (Exception ex)
            {
                MessageBox.Show("发送失败：" + ex.Message, "错误");
            }
        }

        private void butSend4_Click(object sender, EventArgs e)
        {
            try
            {
                // 检查串口是否打开
                if (!serialPort1.IsOpen)
                {
                    MessageBox.Show("请先打开串口！", "提示");
                    return;
                }

                // 获取要发送的文本
                string sendText = txtBoxSend4.Text.Trim();
                if (string.IsNullOrEmpty(sendText))
                {
                    MessageBox.Show("请输入要发送的数据！", "提示");
                    return;
                }

                // 判断是否勾选16进制发送
                if (checkBoxHex4.Checked)
                {
                    // ===== 发送16进制数据 =====
                    // 去掉空格，按每两个字符转换
                    string hex = sendText.Replace(" ", "");
                    if (hex.Length % 2 != 0)
                    {
                        MessageBox.Show("16进制数据长度必须为偶数！", "错误");
                        return;
                    }

                    // 转换为字节数组
                    byte[] data = new byte[hex.Length / 2];
                    for (int i = 0; i < hex.Length; i += 2)
                    {
                        data[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
                    }

                    // 发送16进制数据
                    serialPort1.Write(data, 0, data.Length);
                }
                else
                {
                    // ===== 发送字符串数据 =====
                    byte[] data = System.Text.Encoding.ASCII.GetBytes(sendText);
                    serialPort1.Write(data, 0, data.Length);
                }
            }
            catch (FormatException)
            {
                MessageBox.Show("16进制数据格式错误！请检查是否包含非法字符。", "错误");
            }
            catch (Exception ex)
            {
                MessageBox.Show("发送失败：" + ex.Message, "错误");
            }
        }

        private void butSend5_Click(object sender, EventArgs e)
        {
            try
            {
                // 检查串口是否打开
                if (!serialPort1.IsOpen)
                {
                    MessageBox.Show("请先打开串口！", "提示");
                    return;
                }

                // 获取要发送的文本
                string sendText = txtBoxSend5.Text.Trim();
                if (string.IsNullOrEmpty(sendText))
                {
                    MessageBox.Show("请输入要发送的数据！", "提示");
                    return;
                }

                // 判断是否勾选16进制发送
                if (checkBoxHex5.Checked)
                {
                    // ===== 发送16进制数据 =====
                    // 去掉空格，按每两个字符转换
                    string hex = sendText.Replace(" ", "");
                    if (hex.Length % 2 != 0)
                    {
                        MessageBox.Show("16进制数据长度必须为偶数！", "错误");
                        return;
                    }

                    // 转换为字节数组
                    byte[] data = new byte[hex.Length / 2];
                    for (int i = 0; i < hex.Length; i += 2)
                    {
                        data[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
                    }

                    // 发送16进制数据
                    serialPort1.Write(data, 0, data.Length);
                }
                else
                {
                    // ===== 发送字符串数据 =====
                    byte[] data = System.Text.Encoding.ASCII.GetBytes(sendText);
                    serialPort1.Write(data, 0, data.Length);
                }
            }
            catch (FormatException)
            {
                MessageBox.Show("16进制数据格式错误！请检查是否包含非法字符。", "错误");
            }
            catch (Exception ex)
            {
                MessageBox.Show("发送失败：" + ex.Message, "错误");
            }
        }

        private void butSend6_Click(object sender, EventArgs e)
        {
            try
            {
                // 检查串口是否打开
                if (!serialPort1.IsOpen)
                {
                    MessageBox.Show("请先打开串口！", "提示");
                    return;
                }

                // 获取要发送的文本
                string sendText = txtBoxSend6.Text.Trim();
                if (string.IsNullOrEmpty(sendText))
                {
                    MessageBox.Show("请输入要发送的数据！", "提示");
                    return;
                }

                // 判断是否勾选16进制发送
                if (checkBoxHex6.Checked)
                {
                    // ===== 发送16进制数据 =====
                    // 去掉空格，按每两个字符转换
                    string hex = sendText.Replace(" ", "");
                    if (hex.Length % 2 != 0)
                    {
                        MessageBox.Show("16进制数据长度必须为偶数！", "错误");
                        return;
                    }

                    // 转换为字节数组
                    byte[] data = new byte[hex.Length / 2];
                    for (int i = 0; i < hex.Length; i += 2)
                    {
                        data[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
                    }

                    // 发送16进制数据
                    serialPort1.Write(data, 0, data.Length);
                }
                else
                {
                    // ===== 发送字符串数据 =====
                    byte[] data = System.Text.Encoding.ASCII.GetBytes(sendText);
                    serialPort1.Write(data, 0, data.Length);
                }
            }
            catch (FormatException)
            {
                MessageBox.Show("16进制数据格式错误！请检查是否包含非法字符。", "错误");
            }
            catch (Exception ex)
            {
                MessageBox.Show("发送失败：" + ex.Message, "错误");
            }
        }

        private void butSend7_Click(object sender, EventArgs e)
        {
            try
            {
                // 检查串口是否打开
                if (!serialPort1.IsOpen)
                {
                    MessageBox.Show("请先打开串口！", "提示");
                    return;
                }

                // 获取要发送的文本
                string sendText = txtBoxSend7.Text.Trim();
                if (string.IsNullOrEmpty(sendText))
                {
                    MessageBox.Show("请输入要发送的数据！", "提示");
                    return;
                }

                // 判断是否勾选16进制发送
                if (checkBoxHex7.Checked)
                {
                    // ===== 发送16进制数据 =====
                    // 去掉空格，按每两个字符转换
                    string hex = sendText.Replace(" ", "");
                    if (hex.Length % 2 != 0)
                    {
                        MessageBox.Show("16进制数据长度必须为偶数！", "错误");
                        return;
                    }

                    // 转换为字节数组
                    byte[] data = new byte[hex.Length / 2];
                    for (int i = 0; i < hex.Length; i += 2)
                    {
                        data[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
                    }

                    // 发送16进制数据
                    serialPort1.Write(data, 0, data.Length);
                }
                else
                {
                    // ===== 发送字符串数据 =====
                    byte[] data = System.Text.Encoding.ASCII.GetBytes(sendText);
                    serialPort1.Write(data, 0, data.Length);
                }
            }
            catch (FormatException)
            {
                MessageBox.Show("16进制数据格式错误！请检查是否包含非法字符。", "错误");
            }
            catch (Exception ex)
            {
                MessageBox.Show("发送失败：" + ex.Message, "错误");
            }
        }

        private void butSend8_Click(object sender, EventArgs e)
        {
            try
            {
                // 检查串口是否打开
                if (!serialPort1.IsOpen)
                {
                    MessageBox.Show("请先打开串口！", "提示");
                    return;
                }

                // 获取要发送的文本
                string sendText = txtBoxSend8.Text.Trim();
                if (string.IsNullOrEmpty(sendText))
                {
                    MessageBox.Show("请输入要发送的数据！", "提示");
                    return;
                }

                // 判断是否勾选16进制发送
                if (checkBoxHex8.Checked)
                {
                    // ===== 发送16进制数据 =====
                    // 去掉空格，按每两个字符转换
                    string hex = sendText.Replace(" ", "");
                    if (hex.Length % 2 != 0)
                    {
                        MessageBox.Show("16进制数据长度必须为偶数！", "错误");
                        return;
                    }

                    // 转换为字节数组
                    byte[] data = new byte[hex.Length / 2];
                    for (int i = 0; i < hex.Length; i += 2)
                    {
                        data[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
                    }

                    // 发送16进制数据
                    serialPort1.Write(data, 0, data.Length);
                }
                else
                {
                    // ===== 发送字符串数据 =====
                    byte[] data = System.Text.Encoding.ASCII.GetBytes(sendText);
                    serialPort1.Write(data, 0, data.Length);
                }
            }
            catch (FormatException)
            {
                MessageBox.Show("16进制数据格式错误！请检查是否包含非法字符。", "错误");
            }
            catch (Exception ex)
            {
                MessageBox.Show("发送失败：" + ex.Message, "错误");
            }
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox4.Checked)
            {
                if (!timer2.Enabled)
                {
                    timer2.Start();

                }
            }
            else
            {
                if (timer2.Enabled)
                {
                    timer2.Stop();
                }
            }
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            timer2.Stop();
            xhsend();
            timer2.Start();
        }
        private void xhsend()
        {
            try
            {
                if (!serialPort1.IsOpen)
                {
                    label3.Text = "请先打开串口！";
                    return;
                }

                // 所有控件数组
                TextBox[] sendBoxes = { txtBoxSend1, txtBoxSend2, txtBoxSend3, txtBoxSend4,
                                    txtBoxSend5, txtBoxSend6, txtBoxSend7, txtBoxSend8 };
                CheckBox[] hexBoxes = { checkBoxHex1, checkBoxHex2, checkBoxHex3, checkBoxHex4,
                                    checkBoxHex5, checkBoxHex6, checkBoxHex7, checkBoxHex8 };
                TextBox[] orderBoxes = { txtBoxSx1, txtBoxSx2, txtBoxSx3, txtBoxSx4,
                                     txtBoxSx5, txtBoxSx6, txtBoxSx7, txtBoxSx8 };
                TextBox[] delayBoxes = { txtBoxDealy1, txtBoxDealy2, txtBoxDealy3, txtBoxDealy4,
                                     txtBoxDealy5, txtBoxDealy6, txtBoxDealy7, txtBoxDealy8 };

                // 解析顺序
                List<int> orderList = new List<int>();
                bool allZero = true;

                for (int i = 0; i < 8; i++)
                {
                    string orderText = orderBoxes[i].Text.Trim();
                    if (string.IsNullOrEmpty(orderText)) continue;  // 顺序框为空，跳过

                    int num;
                    if (int.TryParse(orderText, out num))
                    {
                        if (num != 0) allZero = false;
                        int sendIndex = num - 1;

                        // 检查索引是否有效
                        if (sendIndex >= 0 && sendIndex < 8 && !orderList.Contains(sendIndex))
                        {
                            // ===== 关键判断：检查对应的发送框内容是否为空 =====
                            if (!string.IsNullOrEmpty(sendBoxes[sendIndex].Text.Trim()))
                            {
                                orderList.Add(sendIndex);
                            }
                            else
                            {
                                // 发送框为空，跳过
                                label3.Text = "跳过第 " + (i + 1) + " 条指令：发送框 " + (sendIndex + 1) + " 内容为空";
                            }
                        }
                    }
                }

                // 如果全部为0，按1-8自然顺序
                if (allZero && orderList.Count == 0)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        if (!string.IsNullOrEmpty(sendBoxes[i].Text.Trim()))
                        {
                            orderList.Add(i);
                        }
                    }
                }

                if (orderList.Count == 0)
                {
                    label3.Text = "没有可发送的数据！\r\n请填写发送内容或在顺序框中填写编号。";
                    return;
                }

                // 按顺序发送
                for (int i = 0; i < orderList.Count; i++)
                {
                    int idx = orderList[i];
                    string content = sendBoxes[idx].Text.Trim();
                    if (string.IsNullOrEmpty(content)) continue;

                    bool isHex = hexBoxes[idx].Checked;

                    // 读取延时
                    int delay = 0;
                    string delayText = delayBoxes[idx].Text.Trim();
                    if (!string.IsNullOrEmpty(delayText))
                    {
                        int.TryParse(delayText, out delay);
                    }

                    // 发送数据
                    SendData(content, isHex);

                    // 延时（如果不是最后一条）
                    if (delay > 0 && i < orderList.Count - 1)
                    {
                        System.Threading.Thread.Sleep(delay);
                    }
                }

                label3.Text = "发送完成！共发送 " + orderList.Count + " 条指令";
            }
            catch (Exception ex)
            {
                label3.Text = "发送失败：" + ex.Message;
            }
        }

        // 发送单个数据（16进制或字符串）
        private void SendData(string content, bool isHex)
        {
            if (isHex)
            {
                // 16进制发送
                string hex = content.Replace(" ", "").Replace("\t", "");
                if (string.IsNullOrEmpty(hex)) return;

                if (hex.Length % 2 != 0)
                {
                    label3.Text = "16进制数据长度必须为偶数！\r\n当前输入：" + content;
                    return;
                }

                byte[] data = new byte[hex.Length / 2];
                for (int i = 0; i < hex.Length; i += 2)
                {
                    data[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
                }
                serialPort1.Write(data, 0, data.Length);
                // ===== 显示发送日志到 txtReceiveBox1 =====
                string time = DateTime.Now.ToString("HH:mm:ss.fff");
                string hexDisplay = BitConverter.ToString(data).Replace("-", " ");
                AppendToLog(time + " [发送] " + hexDisplay);
            }
            else
            {
                // 字符串发送
                byte[] data = System.Text.Encoding.ASCII.GetBytes(content);
                serialPort1.Write(data, 0, data.Length);
                // ===== 显示发送日志到 txtReceiveBox1 =====
                string time = DateTime.Now.ToString("HH:mm:ss.fff");
                AppendToLog(time + " [发送] " + content);
            }
        }
    }
}
