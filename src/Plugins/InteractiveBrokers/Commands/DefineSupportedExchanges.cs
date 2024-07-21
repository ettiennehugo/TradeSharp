using Microsoft.Extensions.DependencyInjection;
using TradeSharp.CoreUI.Common;
using TradeSharp.CoreUI.Services;

namespace TradeSharp.InteractiveBrokers.Commands
{
  /// <summary>
  /// Structure used to store exchanges supported by Interactive Brokers
  /// </summary>
  internal class Exchange 
  {
    public Exchange() { }
    public string CountryIsoCode { get; set; }
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Tag { get; set; }
    public TimeZoneInfo TimeZone { get; set; }
  }

  /// <summary>
  /// Defines the set of common Exchanges used by Interactive Brokers.
  /// </summary>
  public class DefineSupportedExchanges
  {
    //constants
    private static Exchange[] Exchanges = [
      new Exchange { CountryIsoCode = "US", Id = "SMART", Name = "Smart", Description = "Smart Routing Exchange", Tag = "SMART", TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time") },
      new Exchange { CountryIsoCode = "US", Id = "AMEX", Name = "American Exchange", Description = "American Exchange", Tag = "AMEX", TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time") },
      new Exchange { CountryIsoCode = "US", Id = "ARCA", Name = "Archipelago Exchange", Description = "Archipelago Exchange", Tag = "ARCA", TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time") },
      new Exchange { CountryIsoCode = "US", Id = "BATS", Name = "Bats", Description = "Better Alternative Trading Exchange", Tag = "BATS", TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time") },
      new Exchange { CountryIsoCode = "US", Id = "NASDAQ", Name = "Nasdaq", Description = "Nasdaq", Tag = "NASDAQ", TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time") },
      new Exchange { CountryIsoCode = "US", Id = "NYSE", Name = "New York Stock Exchange", Description = "New York Stock Exchange", Tag = "NYSE", TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time") },
    ];

    //enums


    //types


    //attributes
    private ServiceHost m_serviceHost;

    //constructors
    public DefineSupportedExchanges(ServiceHost serviceHost)
    {
      m_serviceHost = serviceHost;
    }

    //finalizers


    //interface implementations


    //properties


    //methods
    public void Run()
    {
      IProgressDialog progress = m_serviceHost.DialogService.CreateProgressDialog("Validating Instruments", m_serviceHost.Logger);
      progress.StatusMessage = "Validating Instrument definitions against the Contract Cache definitions";
      progress.Progress = 0;
      progress.Minimum = 0;
      progress.Maximum = Exchanges.Count();
      progress.ShowAsync();

      bool definedAnExchange = false;
      ICountryService countryService = m_serviceHost.Host.Services.GetService<ICountryService>()!;
      foreach (Exchange exchange in Exchanges) {
        var definedExchange = m_serviceHost.ExchangeService.Items.FirstOrDefault((e) => e.Name.ToUpper() == exchange.Name.ToUpper() || e.TagStr.Contains(exchange.Tag));
        Data.Country? country = countryService.Items.FirstOrDefault((c) => c.IsoCode == exchange.CountryIsoCode);

        if (country == null)
        {
          progress.LogInformation($"Country \"{exchange.CountryIsoCode}\" required for exchange \"{exchange.Name}\" not defined, please define countries before trying to define exchanges.");
          break;
        }

        if (definedExchange == null)
        {
          progress.LogInformation($"Defining exchange \"{exchange.Name}\"");
          var newExchange = new Data.Exchange(Guid.NewGuid(), Data.Exchange.DefaultAttributes, exchange.Tag, country!.Id, exchange.Name, Array.Empty<string>(), exchange.TimeZone, 2, 1, 1, Guid.Empty, string.Empty);
          m_serviceHost.Database.CreateExchange(newExchange);
          defineStockSessions(newExchange);
          definedAnExchange = true;
        }
        else
          progress.LogInformation($"Skipping exchange \"{exchange.Name}\", already defined.");

        progress.Progress++;
        if (progress.CancellationTokenSource.IsCancellationRequested) break;
      }

      if (definedAnExchange) m_serviceHost.DialogService.PostUIUpdate(() => m_serviceHost.ExchangeService.Refresh());
      progress.Complete = true;
    }

    /// <summary>
    /// Defines the standard stock sessions used to trade Monday to Friday for a given exchange. 
    /// </summary>
    protected void defineStockSessions(Data.Exchange exchange)
    {
      var preMarketMonday = new Data.Session(Guid.NewGuid(), Data.Session.DefaultAttributes, "", "Pre-market Monday", exchange.Id, DayOfWeek.Monday, new TimeOnly(6,0), new TimeOnly(9,29));
      var marketMonday = new Data.Session(Guid.NewGuid(), Data.Session.DefaultAttributes, "", "Monday", exchange.Id, DayOfWeek.Monday, new TimeOnly(9,30), new TimeOnly(15,59));
      var postMarketMonday = new Data.Session(Guid.NewGuid(), Data.Session.DefaultAttributes, "", "Post-market Monday", exchange.Id, DayOfWeek.Monday, new TimeOnly(16,0), new TimeOnly(19,59));
      m_serviceHost.Database.CreateSession(preMarketMonday);
      m_serviceHost.Database.CreateSession(marketMonday);
      m_serviceHost.Database.CreateSession(postMarketMonday);

      var preMarketTuesday = new Data.Session(Guid.NewGuid(), Data.Session.DefaultAttributes, "", "Pre-market Tuesday", exchange.Id, DayOfWeek.Tuesday, new TimeOnly(6, 0), new TimeOnly(9, 29));
      var marketTuesday = new Data.Session(Guid.NewGuid(), Data.Session.DefaultAttributes, "", "Tuesday", exchange.Id, DayOfWeek.Tuesday, new TimeOnly(9, 30), new TimeOnly(15, 59));
      var postMarketTuesday = new Data.Session(Guid.NewGuid(), Data.Session.DefaultAttributes, "", "Post-market Tuesday", exchange.Id, DayOfWeek.Tuesday, new TimeOnly(16, 0), new TimeOnly(19, 59));
      m_serviceHost.Database.CreateSession(preMarketTuesday);
      m_serviceHost.Database.CreateSession(marketTuesday);
      m_serviceHost.Database.CreateSession(postMarketTuesday);

      var preMarketWednesday = new Data.Session(Guid.NewGuid(), Data.Session.DefaultAttributes, "", "Pre-market Wednesday", exchange.Id, DayOfWeek.Wednesday, new TimeOnly(6, 0), new TimeOnly(9, 29));
      var marketWednesday = new Data.Session(Guid.NewGuid(), Data.Session.DefaultAttributes, "", "Wednesday", exchange.Id, DayOfWeek.Wednesday, new TimeOnly(9, 30), new TimeOnly(15, 59));
      var postMarketWednesday = new Data.Session(Guid.NewGuid(), Data.Session.DefaultAttributes, "", "Post-market Wednesday", exchange.Id, DayOfWeek.Wednesday, new TimeOnly(16, 0), new TimeOnly(19, 59));
      m_serviceHost.Database.CreateSession(preMarketWednesday);
      m_serviceHost.Database.CreateSession(marketWednesday);
      m_serviceHost.Database.CreateSession(postMarketWednesday);

      var preMarketThursday = new Data.Session(Guid.NewGuid(), Data.Session.DefaultAttributes, "", "Pre-market Thursday", exchange.Id, DayOfWeek.Thursday, new TimeOnly(6, 0), new TimeOnly(9, 29));
      var marketThursday = new Data.Session(Guid.NewGuid(), Data.Session.DefaultAttributes, "", "Thursday", exchange.Id, DayOfWeek.Thursday, new TimeOnly(9, 30), new TimeOnly(15, 59));
      var postMarketThursday = new Data.Session(Guid.NewGuid(), Data.Session.DefaultAttributes, "", "Post-market Thursday", exchange.Id, DayOfWeek.Thursday, new TimeOnly(16, 0), new TimeOnly(19, 59));
      m_serviceHost.Database.CreateSession(preMarketThursday);
      m_serviceHost.Database.CreateSession(marketThursday);
      m_serviceHost.Database.CreateSession(postMarketThursday);

      var preMarketFriday = new Data.Session(Guid.NewGuid(), Data.Session.DefaultAttributes, "", "Pre-market Friday", exchange.Id, DayOfWeek.Friday, new TimeOnly(6, 0), new TimeOnly(9, 29));
      var marketFriday = new Data.Session(Guid.NewGuid(), Data.Session.DefaultAttributes, "", "Friday", exchange.Id, DayOfWeek.Friday, new TimeOnly(9, 30), new TimeOnly(15, 59));
      var postMarketFriday = new Data.Session(Guid.NewGuid(), Data.Session.DefaultAttributes, "", "Post-market Friday", exchange.Id, DayOfWeek.Friday, new TimeOnly(16, 0), new TimeOnly(19, 59));
      m_serviceHost.Database.CreateSession(preMarketFriday);
      m_serviceHost.Database.CreateSession(marketFriday);
      m_serviceHost.Database.CreateSession(postMarketFriday);
    }
  }
}
