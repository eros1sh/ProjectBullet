using Jint;
using Jint.Native;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace RuriLib.Tests.Functions.Interop
{
    public class JintTests
    {
        [Fact]
        public void InvokeJint_IntegerSum_ReturnInteger()
        {
            var engine = new Engine();
            engine.SetValue("x", 3);
            engine.SetValue("y", 5);
            engine.Execute("var result = x + y;");
            Assert.Equal(8, (int)engine.Global.Get("result").AsNumber());
        }

        [Fact]
        public void InvokeJint_FloatSum_ReturnFloat()
        {
            var engine = new Engine();
            engine.SetValue("x", 3.5f);
            engine.SetValue("y", 5.2f);
            engine.Execute("var result = x + y;");
            Assert.Equal(8.7f, (float)engine.Global.Get("result").AsNumber());
        }

        [Fact]
        public void InvokeJint_StringChain_ReturnString()
        {
            var engine = new Engine();
            engine.SetValue("x", "my");
            engine.SetValue("y", "string");
            engine.Execute("var result = x + y;");
            Assert.Equal("mystring", engine.Global.Get("result").AsString());
        }

        [Fact]
        public void InvokeJint_OutputList_ReturnList()
        {
            var engine = new Engine();
            engine.SetValue("x", "a");
            engine.SetValue("y", "b");
            engine.Execute("var result = [ x, y ];");
            var arr = engine.Global.Get("result").AsArray();
            var outputList = new List<string>();
            for (uint i = 0; i < arr.Length; i++)
                outputList.Add(arr.Get(i.ToString()).AsString());
            Assert.Equal(2, outputList.Count);
            Assert.Equal("a", outputList[0]);
            Assert.Equal("b", outputList[1]);
        }

        [Fact]
        public void InvokeJint_InputList_ReturnString()
        {
            var engine = new Engine();
            engine.SetValue("x", new List<string> { "a", "b" });
            engine.Execute("var result = x[0];");
            Assert.Equal("a", engine.Global.Get("result").AsString());
        }

        [Fact]
        public void InvokeJint_NoOutputs_ReturnNothing()
        {
            var engine = new Engine();
            engine.SetValue("x", 1);
            engine.Execute("var y = x + 1;");
            Assert.True(engine.Global.Get("result").IsUndefined());
        }
    }
}
