using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using HidLibrary;
using Svg;

namespace MacroKeyboardConfiguration
{
    public partial class Form1 : Form
    {
        private List<HidDevice> todosDispositivosHID;
        private List<HidDevice> tecladosMacro;
        private const int VendorIdTecladoMacro = 0x1189; // Vendor ID do seu teclado de macro
        private const int ProductIdTecladoMacro = 0x8890; // Product ID do seu teclado de macro

        public Form1()
        {
            InitializeComponent();
            this.Load += Form1_Load;
            this.KeyDown += Form1_KeyDown;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            RegisterRawInputDevices();
            DetectarTeclados();
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            // O processamento de tecla será feito na mensagem de Raw Input
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            DetectarTeclados();
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_INPUT)
            {
                ProcessRawInput(m.LParam);
            }
            base.WndProc(ref m);
        }

        private void RegisterRawInputDevices()
        {
            RAWINPUTDEVICE[] rid = new RAWINPUTDEVICE[1];

            rid[0].UsagePage = 0x01;
            rid[0].Usage = 0x06;
            rid[0].Flags = RIDEV_INPUTSINK;
            rid[0].Target = Handle;

            if (!RegisterRawInputDevices(rid, (uint)rid.Length, (uint)Marshal.SizeOf(rid[0])))
            {
                throw new Exception("Failed to register raw input devices.");
            }
        }

        private void ProcessRawInput(IntPtr lParam)
        {
            uint dwSize = 0;
            GetRawInputData(lParam, RID_INPUT, IntPtr.Zero, ref dwSize, (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER)));
            if (dwSize == 0) return;

            IntPtr buffer = Marshal.AllocHGlobal((int)dwSize);
            if (GetRawInputData(lParam, RID_INPUT, buffer, ref dwSize, (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER))) != dwSize)
            {
                Marshal.FreeHGlobal(buffer);
                return;
            }

            RAWINPUT raw = (RAWINPUT)Marshal.PtrToStructure(buffer, typeof(RAWINPUT));
            Marshal.FreeHGlobal(buffer);

            if (raw.header.dwType == RIM_TYPEKEYBOARD)
            {
                foreach (var device in todosDispositivosHID)
                {
                    if (device.DevicePath == raw.header.hDevice.ToString())
                    {
                        lblTeclaPressionada.Text = $"Código da tecla pressionada no dispositivo {device.DevicePath}: {raw.keyboard.VKey}";
                        return;
                    }
                }
                lblTeclaPressionada.Text = $"Tecla pressionada em dispositivo desconhecido: {raw.keyboard.VKey}";
            }
        }

        private void DetectarTeclados()
        {
            todosDispositivosHID = HidDevices.Enumerate().ToList();

            tecladosMacro = todosDispositivosHID.Where(device =>
                device.Attributes.VendorId == VendorIdTecladoMacro &&
                device.Attributes.ProductId == ProductIdTecladoMacro).ToList();

            if (tecladosMacro.Count > 0)
            {
                lblStatus.Text = "Teclado detectado!";
                AtualizarIcone("Resources/emoji-smile-fill.svg");
            }
            else
            {
                lblStatus.Text = "Teclado não encontrado.";
                AtualizarIcone("Resources/emoji-frown-fill.svg");
            }
        }

        private void AtualizarIcone(string svgPath)
        {
            var svgDocument = SvgDocument.Open(svgPath);
            int tamanhoDesejado = 100;
            var bitmap = svgDocument.Draw(tamanhoDesejado, tamanhoDesejado);
            pictureBox1.Image = bitmap;
        }

        // P/Invoke definitions
        private const int WM_INPUT = 0x00FF;
        private const uint RID_INPUT = 0x10000003;
        private const uint RIM_TYPEKEYBOARD = 1;
        private const uint RIDEV_INPUTSINK = 0x00000100;

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTDEVICE
        {
            public ushort UsagePage;
            public ushort Usage;
            public uint Flags;
            public IntPtr Target;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWINPUTHEADER
        {
            public uint dwType;
            public uint dwSize;
            public IntPtr hDevice;
            public IntPtr wParam;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct RAWINPUT
        {
            [FieldOffset(0)]
            public RAWINPUTHEADER header;
            [FieldOffset(16)]
            public RAWKEYBOARD keyboard;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RAWKEYBOARD
        {
            public ushort MakeCode;
            public ushort Flags;
            public ushort Reserved;
            public ushort VKey;
            public uint Message;
            public uint ExtraInformation;
        }

        [DllImport("User32.dll", SetLastError = true)]
        private static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] pRawInputDevice, uint uiNumDevices, uint cbSize);

        [DllImport("User32.dll", SetLastError = true)]
        private static extern uint GetRawInputData(IntPtr hRawInput, uint uiCommand, IntPtr pData, ref uint pcbSize, uint cbSizeHeader);
    }
}
