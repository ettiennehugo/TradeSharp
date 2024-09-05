using TradeSharp.Analysis;
using Moq;
using TradeSharp.Analysis.Common;
using Microsoft.Extensions.Logging;

namespace TradeSharp.Analysis.Testing
{
  /// <summary>
  /// Dummy filter used to feed the engine with test data for testing purposes.
  /// </summary>
  public class DummyDataFeedFilter: Filter {
    //constants


    //enums


    //types


    //attributes


    //properties
    public IList<object> Messages { get; }

    //constructors
    public DummyDataFeedFilter(ILogger logger, FilterMode mode, CancellationToken cancellationToken) : base(logger, mode, cancellationToken)
    {
      Messages = new List<object>();
    }

    //finalizers


    //interface implementations


    //methods


  }

  
  [TestClass]
  public class Engine
  {

    //constants


    //enums


    //types


    //attributes
    Mock<IEngineConfiguration> m_configuration;
    ILogger m_logger;
    Analysis.IEngine m_engine;

    //properties


    //constructors
    public Engine()
    {
      var loggerFactory = LoggerFactory.Create(builder =>
      {
        builder.AddConsole();
      });
      m_logger = loggerFactory.CreateLogger<Engine>();
      m_configuration = new Mock<IEngineConfiguration>();
    }


    //finalizers


    //interface implementations


    //methods
    [TestMethod]
    public void Configure_BasicSinglePipe_Success()
    {
      DummyDataFeedFilter? filter1 = null;
      Filter? filter2 = null;
      Filter? filter3 = null;
      IPipe? outputPipe = null;

      m_configuration.Setup(x => x.Compose(It.IsAny<IEngine>())).Callback<IEngine>((engine) =>
      {
        var pipeline = engine.AddPipeline();
        filter1 = new DummyDataFeedFilter(engine.Logger, FilterMode.Synchronous, engine.CancellationTokenSource.Token);
        filter2 = new Filter(engine.Logger, FilterMode.Synchronous, engine.CancellationTokenSource.Token);
        filter3 = new Filter(engine.Logger, FilterMode.Synchronous, engine.CancellationTokenSource.Token);
        pipeline.Add(filter1);
        pipeline.Add(filter2);
        pipeline.Add(filter3);
        filter3.Output = new Pipe(engine.Logger);
        outputPipe = filter3.Output;
      });

      m_engine = new Analysis.Engine(m_logger, m_configuration.Object);
      m_engine.Start();

      Assert.IsNotNull(filter1);
      Assert.IsNotNull(filter2);
      Assert.IsNotNull(filter3);
      Assert.IsNotNull(outputPipe);

      filter1.Messages.Add(1);
      filter1.Messages.Add(2);
      filter1.Messages.Add(3);
      filter1.Messages.Add(4);
      filter1.Messages.Add(5);

      Assert.AreEqual(5, outputPipe.Count);
      Assert.AreEqual(1, outputPipe.Dequeue()!.Data);
      Assert.AreEqual(2, outputPipe.Dequeue()!.Data);
      Assert.AreEqual(3, outputPipe.Dequeue()!.Data);
      Assert.AreEqual(4, outputPipe.Dequeue()!.Data);
      Assert.AreEqual(5, outputPipe.Dequeue()!.Data);
    }

    [TestMethod]
    public void Configure_BasicMultiplePipes_Success()
    {

      DummyDataFeedFilter? filter1_1 = null;
      Filter? filter1_2 = null;
      Filter? filter1_3 = null;

      DummyDataFeedFilter? filter2_1 = null;
      Filter? filter2_2 = null;
      Filter? filter2_3 = null;

      DummyDataFeedFilter? filter3_1 = null;
      Filter? filter3_2 = null;
      Filter? filter3_3 = null;

      IPipe? outputPipe1 = null;
      IPipe? outputPipe2 = null;
      IPipe? outputPipe3 = null;

      m_configuration.Setup(x => x.Compose(It.IsAny<IEngine>())).Callback<IEngine>((engine) =>
      {
        var pipeline1 = engine.AddPipeline();
        var pipeline2 = engine.AddPipeline();
        var pipeline3 = engine.AddPipeline();

        filter1_1 = new DummyDataFeedFilter(engine.Logger, FilterMode.Synchronous, engine.CancellationTokenSource.Token);
        filter1_2 = new Filter(engine.Logger, FilterMode.Synchronous, engine.CancellationTokenSource.Token);
        filter1_3 = new Filter(engine.Logger, FilterMode.Synchronous, engine.CancellationTokenSource.Token);
        pipeline1.Add(filter1_1);
        pipeline1.Add(filter1_2);
        pipeline1.Add(filter1_3);

        filter2_1 = new DummyDataFeedFilter(engine.Logger, FilterMode.Synchronous, engine.CancellationTokenSource.Token);
        filter2_2 = new Filter(engine.Logger, FilterMode.Synchronous, engine.CancellationTokenSource.Token);
        filter2_3 = new Filter(engine.Logger, FilterMode.Synchronous, engine.CancellationTokenSource.Token);
        pipeline2.Add(filter2_1);
        pipeline2.Add(filter2_2);
        pipeline2.Add(filter2_3);

        filter3_1 = new DummyDataFeedFilter(engine.Logger, FilterMode.Synchronous, engine.CancellationTokenSource.Token);
        filter3_2 = new Filter(engine.Logger, FilterMode.Synchronous, engine.CancellationTokenSource.Token);
        filter3_3 = new Filter(engine.Logger, FilterMode.Synchronous, engine.CancellationTokenSource.Token);
        pipeline3.Add(filter3_1);
        pipeline3.Add(filter3_2);
        pipeline3.Add(filter3_3);

        filter1_3.Output = new Pipe(engine.Logger);
        outputPipe1 = filter1_3.Output;

        filter2_3.Output = new Pipe(engine.Logger);
        outputPipe2 = filter2_3.Output;

        filter3_3.Output = new Pipe(engine.Logger);
        outputPipe3 = filter3_3.Output;
      });

      m_engine = new Analysis.Engine(m_logger, m_configuration.Object);
      m_engine.Start();

      Assert.IsNotNull(filter1_1);
      Assert.IsNotNull(filter1_2);
      Assert.IsNotNull(filter1_3);
      Assert.IsNotNull(outputPipe1);

      Assert.IsNotNull(filter2_1);
      Assert.IsNotNull(filter2_2);
      Assert.IsNotNull(filter2_3);
      Assert.IsNotNull(outputPipe2);

      Assert.IsNotNull(filter3_1);
      Assert.IsNotNull(filter3_2);
      Assert.IsNotNull(filter3_3);
      Assert.IsNotNull(outputPipe3);

      filter1_1.Messages.Add(1);
      filter1_1.Messages.Add(2);
      filter1_1.Messages.Add(3);
      filter1_1.Messages.Add(4);
      filter1_1.Messages.Add(5);

      filter2_1.Messages.Add(6);
      filter2_1.Messages.Add(7);
      filter2_1.Messages.Add(8);
      filter2_1.Messages.Add(9);
      filter2_1.Messages.Add(10);

      filter3_1.Messages.Add(11);
      filter3_1.Messages.Add(12);
      filter3_1.Messages.Add(13);
      filter3_1.Messages.Add(14);
      filter3_1.Messages.Add(15);

      Assert.AreEqual(5, outputPipe1.Count);
      Assert.AreEqual(1, outputPipe1.Dequeue()!.Data);
      Assert.AreEqual(2, outputPipe1.Dequeue()!.Data);
      Assert.AreEqual(3, outputPipe1.Dequeue()!.Data);
      Assert.AreEqual(4, outputPipe1.Dequeue()!.Data);
      Assert.AreEqual(5, outputPipe1.Dequeue()!.Data);

      Assert.AreEqual(5, outputPipe2.Count);
      Assert.AreEqual(6, outputPipe2.Dequeue()!.Data);
      Assert.AreEqual(7, outputPipe2.Dequeue()!.Data);
      Assert.AreEqual(8, outputPipe2.Dequeue()!.Data);
      Assert.AreEqual(9, outputPipe2.Dequeue()!.Data);
      Assert.AreEqual(10, outputPipe2.Dequeue()!.Data);

      Assert.AreEqual(5, outputPipe3.Count);
      Assert.AreEqual(11, outputPipe3.Dequeue()!.Data);
      Assert.AreEqual(12, outputPipe3.Dequeue()!.Data);
      Assert.AreEqual(13, outputPipe3.Dequeue()!.Data);
      Assert.AreEqual(14, outputPipe3.Dequeue()!.Data);
      Assert.AreEqual(15, outputPipe3.Dequeue()!.Data);
    }

    [TestMethod]
    public void Validate_Pipeline_Success()
    {

      //TODO - validate pipeline success

    }

    [TestMethod]
    public void OnceOff_FeedData_Success()
    {

      //TODO - perform a once-off run where data is fed into the engine and the engine is run once through it

    }

    [TestMethod]
    public void ContiniousLoop_FeedData_Failure()
    {

      //TODO - perform continious loop where data is fed into the engine and the engine keeps on running through it

    }
  }
}