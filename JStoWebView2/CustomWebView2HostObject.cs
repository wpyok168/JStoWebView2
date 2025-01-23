using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JStoWebView2
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ComVisible(true)]
    public class CustomWebView2HostObject
    {
        public string TestCalcAddByCsharpMethod(int num1, int num2, string message)
        {
            MessageBox.Show($"C#方法接收到J传入的参数 num1={num1}，num2={num2}，message={message}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);

            return "计算结果为:" + (num1 + num2);
        }
    }


    // Bridge and BridgeAnotherClass are C# classes that implement IDispatch and works with AddHostObjectToScript.
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ComVisible(true)]
    public class BridgeAnotherClass
    {
        // Sample property.
        public string Prop { get; set; } = "Example";
    }

    [ClassInterface(ClassInterfaceType.AutoDual)]
    [ComVisible(true)]
    public class Bridge
    {
        public string Func(string param)
        {
            return "Example: " + param;
        }

        public BridgeAnotherClass AnotherObject { get; set; } = new BridgeAnotherClass();

        // Sample indexed property.
        [System.Runtime.CompilerServices.IndexerName("Items")]
        public string this[int index]
        {
            get { return m_dictionary[index]; }
            set { m_dictionary[index] = value; }
        }
        private Dictionary<int, string> m_dictionary = new Dictionary<int, string>();
    }
}
