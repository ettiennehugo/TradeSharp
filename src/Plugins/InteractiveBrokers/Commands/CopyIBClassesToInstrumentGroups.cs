using TradeSharp.CoreUI.Common;
using TradeSharp.Data;
using IBApi;
using System.Text.Json;

namespace TradeSharp.InteractiveBrokers.Commands
{
  /// <summary>
  /// Meta-data stored in the Tag of InstrumentGroup objects created by this command.
  /// </summary>
  public class InstrumentGroupMetaData
  {
    public int Version { get; set; } = 0;
    public string Industry { get; set; } = "";
    public string Category { get; set; } = "";
    public string SubCategory { get; set; } = "";
  }

  /// <summary>
  /// Command class to copy Interactive Brokers classes to instrument groups.
  /// </summary>
  public class CopyIBClassesToInstrumentGroups
  {
    //constants
    public const int CurrentMetaDataVersion = 1;

    //enums


    //types


    //attributes
    private InstrumentAdapter m_adapter;

    //constructors
    public CopyIBClassesToInstrumentGroups(InstrumentAdapter adapter)
    {
      m_adapter = adapter;
    }

    //finalizers


    //interface implementations


    //properties


    //methods
    public void Run()
    {
      IProgressDialog progress = m_adapter.m_dialogService.CreateProgressDialog("Copy Interactive Broker Classes to Instrument Groups", m_adapter.m_logger);
      progress.StatusMessage = "Copy Interactive Broker Classes to Instrument Groups";
      progress.Progress = 0;
      progress.Minimum = 0;
      List<Contract> contracts = m_adapter.m_serviceHost.Cache.GetContracts();
      progress.Maximum = contracts.Count;
      progress.ShowAsync();
      
      //define root group associated with the interactive borker classifications
      InstrumentGroup? rootGroup = m_adapter.m_instrumentGroupService.Items.FirstOrDefault((g) => g.Name == Constants.DefaultRootInstrumentGroupName && g.Tag == Constants.DefaultRootInstrumentGroupTag);
      if (rootGroup == null)
      {
        rootGroup = new InstrumentGroup(Guid.NewGuid(), Attributes.None /* not editable */, Constants.DefaultRootInstrumentGroupTag, InstrumentGroup.InstrumentGroupRoot, Constants.DefaultRootInstrumentGroupName, Array.Empty<string>(), Constants.DefaultRootInstrumentGroupName, "", Array.Empty<string>());
        m_adapter.m_database.CreateInstrumentGroup(rootGroup);
        progress.LogInformation($"Created root instrument group \"{Constants.DefaultRootInstrumentGroupName}\"");
      }

      //determine the list of instrument groups already associated with the Interactive Brokers classifications
      List<Tuple<string, string, string, InstrumentGroup>> definedInstrumentGroups = new List<Tuple<string, string, string, InstrumentGroup>>();
      foreach (var instrumentGroup in m_adapter.m_instrumentGroupService.Items)
      {
        //check whether this instrument group ultimately has the IB root as parent
        var currentGroup = instrumentGroup;
        while (currentGroup != null && (currentGroup.ParentId != InstrumentGroup.InstrumentGroupRoot || currentGroup.ParentId != rootGroup.Id))
          currentGroup = m_adapter.m_instrumentGroupService.Items.FirstOrDefault(g => g.Id == currentGroup.ParentId);

        //add instrument group to the list of defined instrument groups
        if (currentGroup != null && currentGroup.ParentId == rootGroup.Id)
        {
          var metaData = deserializeMetaData(instrumentGroup.Tag);
          definedInstrumentGroups.Add(new Tuple<string, string, string, InstrumentGroup>(metaData!.Industry, metaData!.Category, metaData!.SubCategory, instrumentGroup));
        }
      }

      //determine the set of industries, categories and sub-categories that must be defined
      List<Tuple<string, string, string>> requiredInstrumentGroups = new List<Tuple<string, string, string>>();
      foreach (var contract in contracts)
      {
        if (contract is ContractStock contractStock)
        {
          if (contractStock.Industry != "" && requiredInstrumentGroups.FirstOrDefault((g) => g.Item1 == contractStock.Industry && g.Item2 == contractStock.Category && g.Item3 == contractStock.Subcategory) == null)
            requiredInstrumentGroups.Add(new Tuple<string, string, string>(contractStock.Industry, contractStock.Category, contractStock.Subcategory));
        }

        progress.Progress++;
        if (progress.CancellationTokenSource.IsCancellationRequested) return;
      }

      progress.StatusMessage = $"Found {requiredInstrumentGroups.Count} Interactive Broker Classes to define as Instrument Groups";
      progress.Maximum += requiredInstrumentGroups.Count;

      //determine the sets of industries, categories and sub-categories we need to define
      int copiedGroups = 0;
      foreach (var requiredInstrumentGroup in requiredInstrumentGroups)
      {
        //try to find the industry, category and sub-category in the already defined instrument groups
        var industry = definedInstrumentGroups.FirstOrDefault((g) => g.Item4.Name == requiredInstrumentGroup.Item1)?.Item4;
        var category = definedInstrumentGroups.FirstOrDefault((g) => g.Item4.Name == requiredInstrumentGroup.Item2)?.Item4;
        var subCategory = definedInstrumentGroups.FirstOrDefault((g) => g.Item4.Name == requiredInstrumentGroup.Item3)?.Item4;

        //define what is not found
        if (industry == null)
        {
          industry = new InstrumentGroup(Guid.NewGuid(), Attributes.None, serializeMetaData(requiredInstrumentGroup.Item1, requiredInstrumentGroup.Item2, requiredInstrumentGroup.Item3), rootGroup.Id, requiredInstrumentGroup.Item1, Array.Empty<string>(), requiredInstrumentGroup.Item1, requiredInstrumentGroup.Item1, Array.Empty<string>());
          m_adapter.m_database.CreateInstrumentGroup(industry);
          definedInstrumentGroups.Add(new(requiredInstrumentGroup.Item1, requiredInstrumentGroup.Item2, requiredInstrumentGroup.Item3, industry));
          copiedGroups++;
        }

        if (category == null)
        {
          category = new InstrumentGroup(Guid.NewGuid(), Attributes.None, serializeMetaData(requiredInstrumentGroup.Item1, requiredInstrumentGroup.Item2, requiredInstrumentGroup.Item3), industry.Id, requiredInstrumentGroup.Item2, Array.Empty<string>(), requiredInstrumentGroup.Item2, requiredInstrumentGroup.Item2, Array.Empty<string>());
          m_adapter.m_database.CreateInstrumentGroup(category);
          definedInstrumentGroups.Add(new(requiredInstrumentGroup.Item1, requiredInstrumentGroup.Item2, requiredInstrumentGroup.Item3, category));
          copiedGroups++;
        }

        if (subCategory == null)
        {
          subCategory = new InstrumentGroup(Guid.NewGuid(), Attributes.None, serializeMetaData(requiredInstrumentGroup.Item1, requiredInstrumentGroup.Item2, requiredInstrumentGroup.Item3), category.Id, requiredInstrumentGroup.Item3, Array.Empty<string>(), requiredInstrumentGroup.Item3, requiredInstrumentGroup.Item3, Array.Empty<string>());
          m_adapter.m_database.CreateInstrumentGroup(subCategory);
          definedInstrumentGroups.Add(new(requiredInstrumentGroup.Item1, requiredInstrumentGroup.Item2, requiredInstrumentGroup.Item3, subCategory));
          copiedGroups++;
        }

        progress.Progress++;
        if (progress.CancellationTokenSource.IsCancellationRequested) return;
      }

      progress.StatusMessage = $"Copied {copiedGroups} groups, total defined groups are now {definedInstrumentGroups.Count}";
      progress.Complete = true;
      
      //kick off task to refresh the instrument group definitions
      if (copiedGroups > 0) Task.Run(m_adapter.m_instrumentGroupService.Refresh);
    }

    private string serializeMetaData(string industry, string category, string subCategory)
    {
      return JsonSerializer.Serialize(new InstrumentGroupMetaData { Version = CurrentMetaDataVersion, Industry = industry, Category = category, SubCategory = subCategory });
    }

    private InstrumentGroupMetaData? deserializeMetaData(string value)
    {
      return JsonSerializer.Deserialize<InstrumentGroupMetaData>(value);
    }
  }
}
