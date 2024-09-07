using Moq;
using System.Collections.Concurrent;
using TradeSharp.Analysis.Common;
using Microsoft.Extensions.Logging;
using System.Data;

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
    public bool CompleteOnLast { get; set; }
    public ConcurrentQueue<object> Queue { get; }

    //constructors
    public DummyDataFeedFilter(string name, ILogger logger, FilterMode mode, CancellationToken cancellationToken) : base(name, logger, mode, cancellationToken)
    {
      Queue = new ConcurrentQueue<object>();
    }

    //finalizers


    //interface implementations


    //methods
    public override bool Evaluate()
    {
      if (Queue.TryDequeue(out object? value))
      {
        Output.Enqueue(new Message(value));
        return true;
      }

      if (CompleteOnLast)
        Status = ExecutionStatus.Completed;

      return false;
    }
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
        var pipeline = engine.AddPipeline("pipeline");
        filter1 = new DummyDataFeedFilter("filter1", engine.Logger, FilterMode.Synchronous, engine.CancellationTokenSource.Token);
        filter2 = new Filter("filter2", engine.Logger, FilterMode.Synchronous, engine.CancellationTokenSource.Token);
        filter3 = new Filter("filter3", engine.Logger, FilterMode.Synchronous, engine.CancellationTokenSource.Token);
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

      //check that composition was correctly setup
      Assert.AreEqual(1, m_engine.Pipelines.Count);
      Assert.AreEqual(3, m_engine.Pipelines[0].Composition.Count);
      Assert.AreEqual(filter1.Output.End, filter2);
      Assert.AreEqual(filter2.Input.Source, filter1);
      Assert.AreEqual(filter2.Output.End, filter3);
      Assert.AreEqual(filter3.Input.Source, filter2);

      //check that data is pushed through the pipe correctly
      filter1.Queue.Enqueue(1);
      filter1.Queue.Enqueue(2);
      filter1.Queue.Enqueue(3);
      filter1.Queue.Enqueue(4);
      filter1.Queue.Enqueue(5);

      Thread.Sleep(1);   //allow pipeline thread to push data through to the output pipe

      Assert.AreEqual(5, outputPipe.Count);
      Assert.AreEqual(1, outputPipe.Dequeue()!.Data);
      Assert.AreEqual(2, outputPipe.Dequeue()!.Data);
      Assert.AreEqual(3, outputPipe.Dequeue()!.Data);
      Assert.AreEqual(4, outputPipe.Dequeue()!.Data);
      Assert.AreEqual(5, outputPipe.Dequeue()!.Data);

      m_engine.CancellationTokenSource.Cancel();
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
        var pipeline1 = engine.AddPipeline("pipeline1");
        var pipeline2 = engine.AddPipeline("pipeline2");
        var pipeline3 = engine.AddPipeline("pipeline3");

        filter1_1 = new DummyDataFeedFilter("filter1_1", engine.Logger, FilterMode.Synchronous, engine.CancellationTokenSource.Token);
        filter1_2 = new Filter("filter1_2", engine.Logger, FilterMode.Synchronous, engine.CancellationTokenSource.Token);
        filter1_3 = new Filter("filter1_3", engine.Logger, FilterMode.Synchronous, engine.CancellationTokenSource.Token);
        pipeline1.Add(filter1_1);
        pipeline1.Add(filter1_2);
        pipeline1.Add(filter1_3);

        filter2_1 = new DummyDataFeedFilter("filter2_1", engine.Logger, FilterMode.Synchronous, engine.CancellationTokenSource.Token);
        filter2_2 = new Filter("filter2_2", engine.Logger, FilterMode.Synchronous, engine.CancellationTokenSource.Token);
        filter2_3 = new Filter("filter2_3", engine.Logger, FilterMode.Synchronous, engine.CancellationTokenSource.Token);
        pipeline2.Add(filter2_1);
        pipeline2.Add(filter2_2);
        pipeline2.Add(filter2_3);

        filter3_1 = new DummyDataFeedFilter("filter3_1", engine.Logger, FilterMode.Synchronous, engine.CancellationTokenSource.Token);
        filter3_2 = new Filter("filter3_2", engine.Logger, FilterMode.Synchronous, engine.CancellationTokenSource.Token);
        filter3_3 = new Filter("filter3_3", engine.Logger, FilterMode.Synchronous, engine.CancellationTokenSource.Token);
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

      //check that composition was correctly setup
      Assert.AreEqual(3, m_engine.Pipelines.Count);

      Assert.IsNotNull(filter1_1);
      Assert.IsNotNull(filter1_2);
      Assert.IsNotNull(filter1_3);
      Assert.IsNotNull(outputPipe1);

      Assert.AreEqual(3, m_engine.Pipelines[0].Composition.Count);
      Assert.AreEqual(filter1_1.Output.End, filter1_2);
      Assert.AreEqual(filter1_2.Input.Source, filter1_1);
      Assert.AreEqual(filter1_2.Output.End, filter1_3);
      Assert.AreEqual(filter1_3.Input.Source, filter1_2);

      Assert.IsNotNull(filter2_1);
      Assert.IsNotNull(filter2_2);
      Assert.IsNotNull(filter2_3);
      Assert.IsNotNull(outputPipe2);

      Assert.AreEqual(3, m_engine.Pipelines[1].Composition.Count);
      Assert.AreEqual(filter2_1.Output.End, filter2_2);
      Assert.AreEqual(filter2_2.Input.Source, filter2_1);
      Assert.AreEqual(filter2_2.Output.End, filter2_3);
      Assert.AreEqual(filter2_3.Input.Source, filter2_2);

      Assert.IsNotNull(filter3_1);
      Assert.IsNotNull(filter3_2);
      Assert.IsNotNull(filter3_3);
      Assert.IsNotNull(outputPipe3);

      Assert.AreEqual(3, m_engine.Pipelines[2].Composition.Count);
      Assert.AreEqual(filter3_1.Output.End, filter3_2);
      Assert.AreEqual(filter3_2.Input.Source, filter3_1);
      Assert.AreEqual(filter3_2.Output.End, filter3_3);
      Assert.AreEqual(filter3_3.Input.Source, filter3_2);

      //check that data is pushed through the pipe correctly
      filter1_1.Queue.Enqueue(1);
      filter1_1.Queue.Enqueue(2);
      filter1_1.Queue.Enqueue(3);
      filter1_1.Queue.Enqueue(4);
      filter1_1.Queue.Enqueue(5);

      filter2_1.Queue.Enqueue(6);
      filter2_1.Queue.Enqueue(7);
      filter2_1.Queue.Enqueue(8);
      filter2_1.Queue.Enqueue(9);
      filter2_1.Queue.Enqueue(10);

      filter3_1.Queue.Enqueue(11);
      filter3_1.Queue.Enqueue(12);
      filter3_1.Queue.Enqueue(13);
      filter3_1.Queue.Enqueue(14);
      filter3_1.Queue.Enqueue(15);

      Thread.Sleep(1); //allow the pipeline threads to push data through to the output pipe

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

      m_engine.CancellationTokenSource.Cancel();
    }

    [TestMethod]
    public void OnceOff_FeedData_Success()
    {
      IPipeline? pipeline = null;
      DummyDataFeedFilter? filter1 = null;
      Filter? filter2 = null;
      Filter? filter3 = null;
      IPipe? outputPipe = null;

      m_configuration.Setup(x => x.Compose(It.IsAny<IEngine>())).Callback<IEngine>((engine) =>
      {
        pipeline = engine.AddPipeline("pipeline");
        filter1 = new DummyDataFeedFilter("filter1", engine.Logger, FilterMode.Synchronous, engine.CancellationTokenSource.Token);
        filter2 = new Filter("filter2", engine.Logger, FilterMode.Synchronous, engine.CancellationTokenSource.Token);
        filter3 = new Filter("filter3", engine.Logger, FilterMode.Synchronous, engine.CancellationTokenSource.Token);
        pipeline.Add(filter1);
        pipeline.Add(filter2);
        pipeline.Add(filter3);
        filter3.Output = new Pipe(engine.Logger);
        outputPipe = filter3.Output;
      });

      m_engine = new Analysis.Engine(m_logger, m_configuration.Object);
      m_engine.Start();

      Assert.IsNotNull(pipeline);
      Assert.IsNotNull(filter1);
      Assert.IsNotNull(filter2);
      Assert.IsNotNull(filter3);
      Assert.IsNotNull(outputPipe);

      filter1.Queue.Enqueue(1);
      filter1.Queue.Enqueue(2);
      filter1.Queue.Enqueue(3);
      filter1.Queue.Enqueue(4);
      filter1.Queue.Enqueue(5);
      filter1.CompleteOnLast = true;

      Thread.Sleep(1); //allow the pipeline threads to push data through to the output pipe

      Assert.AreEqual(filter1!.Status, ExecutionStatus.Completed);
      Assert.AreEqual(filter2!.Status, ExecutionStatus.Completed);
      Assert.AreEqual(filter3!.Status, ExecutionStatus.Completed);
      Assert.AreEqual(pipeline!.Status, ExecutionStatus.Completed);

      Assert.AreEqual(5, outputPipe.Count); //output pipe should have received all the data
    }
  }
}