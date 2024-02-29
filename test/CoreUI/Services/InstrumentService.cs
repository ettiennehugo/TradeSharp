using Microsoft.Extensions.Logging;
using TradeSharp.Data;
using TradeSharp.CoreUI.Repositories;
using TradeSharp.CoreUI.Services;
using Microsoft.VisualBasic;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Drawing;
using System.IO;
using System.Xml.Linq;
using System;

namespace TradeSharp.CoreUI.Testing.Services
{
  [TestClass]
  public class InstrumentService
  {
    //constants

    string[] jsonWithIds = [


    ];

    string[] jsonWithParentNames = [


    ];

    //enums


    //types


    //attributes
    private Mock<ILogger<CoreUI.Services.InstrumentService>> m_logger;
    private Mock<IDatabase> m_database;
    private Mock<IInstrumentRepository> m_instrumentRepository;
    private Mock<IDialogService> m_dialogService;
    private CoreUI.Services.InstrumentService m_instrumentService;

    //constructors
    public InstrumentService()
    {
       m_logger = new Mock<ILogger<CoreUI.Services.InstrumentService>>(MockBehavior.Strict);
       m_database = new Mock<IDatabase>(MockBehavior.Strict);
       m_instrumentRepository = new Mock<IInstrumentRepository>(MockBehavior.Strict);
       m_dialogService = new Mock<IDialogService>(MockBehavior.Strict);
       m_instrumentService = new CoreUI.Services.InstrumentService(m_logger.Object, m_database.Object, m_instrumentRepository.Object, m_dialogService.Object);



    }

    //finalizers
    ~InstrumentService()
    {

    }

    //interface implementations


    //properties


    //methods
    [TestMethod]
    public void Import_Skip_Success()
    {

    }

    [TestMethod]
    public void Import_Replace_Success()
    {

    }

    [TestMethod]
    public void Import_Update_Success()
    {

    }

    [TestMethod]
    public void Export()
    {

    }
  }
}
