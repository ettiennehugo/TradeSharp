namespace TradeSharp.Data.Testing
{
  [TestClass]
  public class DataStream
  {
    //constants


    //enums


    //types


    //attributes
    List<double> m_testData;
    DataStream<double> m_dataStream;

    //constructors
    public DataStream()
    {
      m_testData = new List<double> { 1.0, 2.0, 3.0, 4.0, 5.0 };
      m_dataStream = new DataStream<double>();
      m_dataStream.Data = m_testData;
    }

    //finalizers


    //interface implementations


    //properties


    //methods
    [TestMethod]
    [DataRow(0, 0)]
    [DataRow(1, 0)]
    [DataRow(2, 1)]
    [DataRow(3, 2)]
    [DataRow(4, 3)]
    [DataRow(4, 4)]
    public void IndexOperator_CheckIndexInRange_Success(int currentBar, int index)
    {
      m_dataStream.CurrentBar = currentBar;
      double data = m_dataStream[index];
    }

    [TestMethod]
    [DataRow(0, -1)]
    [DataRow(0, 1)]
    [DataRow(1, 2)]
    [DataRow(2, 5)]
    [DataRow(2, 6)]
    [ExpectedException(typeof(ArgumentOutOfRangeException))]
    public void IndexOperator_CheckIndexOutOfRange_Success(int currentBar, int index)
    {
      m_dataStream.CurrentBar = currentBar;
      double data = m_dataStream[index];
    }
  }
}
