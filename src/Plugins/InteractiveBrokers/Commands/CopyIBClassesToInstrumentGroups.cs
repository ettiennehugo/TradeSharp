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


    //enums


    //types


    //attributes
    private ServiceHost m_serviceHost;

    //constructors
    public CopyIBClassesToInstrumentGroups(ServiceHost serviceHost)
    {
      m_serviceHost = serviceHost;
    }

    //finalizers


    //interface implementations


    //properties


    //methods
    public void Run()
    {
      IProgressDialog progress = m_serviceHost.DialogService.CreateProgressDialog("Copy Interactive Broker Classes to Instrument Groups", m_serviceHost.Logger);
      progress.StatusMessage = "Copy Interactive Broker Classes to Instrument Groups";
      progress.Progress = 0;
      progress.Minimum = 0;
      List<Contract> contracts = m_serviceHost.Cache.GetContracts();
      progress.Maximum = m_serviceHost.InstrumentGroupService.Items.Count;
      progress.Maximum += contracts.Count;
      progress.ShowAsync();
      
      //define root group associated with the interactive broker classifications
      InstrumentGroup? rootGroup = m_serviceHost.InstrumentGroupService.Items.FirstOrDefault((g) => g.Name == Constants.DefaultRootInstrumentGroupName && g.TagStr.Contains(Constants.DefaultRootInstrumentGroupTag));
      if (rootGroup == null)
      {
        rootGroup = new InstrumentGroup(Guid.NewGuid(), Attributes.None /* not editable */, Constants.DefaultRootInstrumentGroupTag, InstrumentGroup.InstrumentGroupRoot, Constants.DefaultRootInstrumentGroupName, Array.Empty<string>(), Constants.DefaultRootInstrumentGroupName, "", Array.Empty<string>());
        m_serviceHost.Database.CreateInstrumentGroup(rootGroup);
        progress.LogInformation($"Created root instrument group \"{Constants.DefaultRootInstrumentGroupName}\"");
      }

      //determine the list of instrument groups already associated with the Interactive Brokers classifications
      List<Tuple<string, string, string, InstrumentGroup>> definedInstrumentGroups = new List<Tuple<string, string, string, InstrumentGroup>>();
      foreach (var instrumentGroup in m_serviceHost.InstrumentGroupService.Items)
      {
        //check whether this instrument group ultimately has the IB root as parent
        var currentGroup = instrumentGroup;
        while (currentGroup != null && (currentGroup.ParentId != InstrumentGroup.InstrumentGroupRoot || currentGroup.ParentId != rootGroup.Id))
          currentGroup = m_serviceHost.InstrumentGroupService.Items.FirstOrDefault(g => g.Id == currentGroup.ParentId);

        //add instrument group to the list of defined instrument groups
        if (currentGroup != null && currentGroup.ParentId == rootGroup.Id)
        {
          var metaData = deserializeMetaData(instrumentGroup, progress);
          definedInstrumentGroups.Add(new Tuple<string, string, string, InstrumentGroup>(metaData!.Industry, metaData!.Category, metaData!.SubCategory, instrumentGroup));       
        }
        
        progress.Progress++;
        if (progress.CancellationTokenSource.IsCancellationRequested) return;
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
          m_serviceHost.Database.CreateInstrumentGroup(industry);
          definedInstrumentGroups.Add(new(requiredInstrumentGroup.Item1, requiredInstrumentGroup.Item2, requiredInstrumentGroup.Item3, industry));
          copiedGroups++;
        }

        if (category == null)
        {
          category = new InstrumentGroup(Guid.NewGuid(), Attributes.None, serializeMetaData(requiredInstrumentGroup.Item1, requiredInstrumentGroup.Item2, requiredInstrumentGroup.Item3), industry.Id, requiredInstrumentGroup.Item2, Array.Empty<string>(), requiredInstrumentGroup.Item2, requiredInstrumentGroup.Item2, Array.Empty<string>());
          m_serviceHost.Database.CreateInstrumentGroup(category);
          definedInstrumentGroups.Add(new(requiredInstrumentGroup.Item1, requiredInstrumentGroup.Item2, requiredInstrumentGroup.Item3, category));
          copiedGroups++;
        }

        if (subCategory == null)
        {
          subCategory = new InstrumentGroup(Guid.NewGuid(), Attributes.None, serializeMetaData(requiredInstrumentGroup.Item1, requiredInstrumentGroup.Item2, requiredInstrumentGroup.Item3), category.Id, requiredInstrumentGroup.Item3, Array.Empty<string>(), requiredInstrumentGroup.Item3, requiredInstrumentGroup.Item3, Array.Empty<string>());
          m_serviceHost.Database.CreateInstrumentGroup(subCategory);
          definedInstrumentGroups.Add(new(requiredInstrumentGroup.Item1, requiredInstrumentGroup.Item2, requiredInstrumentGroup.Item3, subCategory));
          copiedGroups++;
        }

        progress.Progress++;
        if (progress.CancellationTokenSource.IsCancellationRequested) return;
      }

      progress.StatusMessage = $"Copied {copiedGroups} groups, total defined groups are now {definedInstrumentGroups.Count}";
      progress.Complete = true;
      
      //kick off task to refresh the instrument group definitions
      if (copiedGroups > 0) Task.Run(m_serviceHost.InstrumentGroupService.Refresh);
    }

    private string serializeMetaData(string industry, string category, string subCategory)
    {
      Common.TagValue tagValue = new Common.TagValue();
      tagValue.Update(Constants.TagDataId, DateTime.UtcNow, Constants.TagDataVersionMajor, Constants.TagDataVersionMinor, Constants.TagDataVersionPatch, JsonSerializer.Serialize(new InstrumentGroupMetaData { Industry = industry, Category = category, SubCategory = subCategory }));
      return tagValue.ToJson();
    }

    private InstrumentGroupMetaData? deserializeMetaData(InstrumentGroup instrumentGroup, IProgressDialog progressDialog)
    {
      Common.TagEntry? entry = instrumentGroup.Tag.BestMatch(Constants.TagDataId, Constants.TagDataVersionMajor, Constants.TagDataVersionMinor, Constants.TagDataVersionPatch);

      if (entry == null) return null;

      try
      {
        return JsonSerializer.Deserialize<InstrumentGroupMetaData>(entry.Value);
      }
      catch (Exception e)
      {
        progressDialog.LogError($"Failed to deserialize meta-data for instrument group {instrumentGroup.Name}: {e.Message}");
        return null;
      }
    }
  }
}
