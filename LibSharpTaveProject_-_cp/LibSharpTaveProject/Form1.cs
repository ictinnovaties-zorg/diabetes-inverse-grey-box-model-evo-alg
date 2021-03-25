using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using LibSharpTave;

namespace LibSharpTaveProject {
    public partial class Form1 : Form {
        Octave octave;
        string PathToScripts = null;
        public Form1() {
            InitializeComponent();
            PathToScripts = Application.ExecutablePath.Substring(0, Application.ExecutablePath.LastIndexOf("\\") + 1);
            octave = new Octave(null, false);
            this.textBox1.Text = octave.OctaveEntryText;
            ExecuteSomething();
        }

        private void ExecuteSomething() {
            octave.ExecuteCommand("a=[1,2;3,4];");
            octave.ExecuteCommand("result=a';");
            double[][] m = octave.GetMatrix("result");
            this.textBox1.Text += "Size of a' is " + m.Length + "x" + m[0].Length + "\r\n";
            for (int i = 0; i < m.Length; i++) {
                for (int j = 0; j < m[0].Length; j++) {
                    this.textBox1.Text += m[i][j].ToString("0.000") + "\t";
                }
                this.textBox1.Text += "\r\n";
            }
        }
    }
}
