using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using TradeSharp.WinCoreUI.Common;

namespace TradeSharp.WinCoreUI.Testing
{
  public enum TestEnum {
    [System.ComponentModel.Description("Value0 description")]
    Value0,
    Value1,
    [System.ComponentModel.Description("Value2 description")]
    Value2,
    Value3
  }

  [TestClass]
  public class EnumValueDescriptionConverter
  {
    public TradeSharp.WinCoreUI.Common.EnumValueDescriptionConverter m_valueConverter;

    public EnumValueDescriptionConverter()
    {
      m_valueConverter = new Common.EnumValueDescriptionConverter();
    }
    
    [TestMethod]
    public void Convert_BasedOnNameOrDescription_Success()
    {
      Assert.AreEqual("Value0 description", m_valueConverter.Convert(TestEnum.Value0, typeof(string), 0, "en"), "Description for value0 is not retruned.");
      Assert.AreEqual("Value1", m_valueConverter.Convert(TestEnum.Value1, typeof(string), 0, "en"), "Name for value1 is not retruned.");
    }

    [TestMethod]
    public void ConvertBack_BasedOnNameOrDescription_Success()
    {
      Assert.AreEqual(TestEnum.Value0, m_valueConverter.ConvertBack("Value0 description", typeof(TestEnum), 0, "en"), "Value0 not deduced from description");
      Assert.AreEqual(TestEnum.Value0, m_valueConverter.ConvertBack("Value0", typeof(TestEnum), 0, "en"), "Value0 not deduced from name");
      Assert.AreEqual(TestEnum.Value1, m_valueConverter.ConvertBack("Value1", typeof(TestEnum), 0, "en"), "Value1 not deduced from name");
    }
  }
}
