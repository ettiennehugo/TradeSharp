using TradeSharp.Data;
using IBApi;
using TradeSharp.CoreUI.Common;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace TradeSharp.InteractiveBrokers.Commands
{

  /// <summary>
  /// Checks the instrument groups against the IB contract definitions to ensure they are in sync.
  /// </summary>
  internal class InstrumentGroupValidation
  {
    //Word separators used to split the contract group names into words for matching
    public static string[] WordSeparators = new string[] { " ", "\t", ",", "-", "_", ".", "/", "\\" };

    public enum MatchesOn
    {
      None,
      Industry,
      Category,
      Subcategory
    }

    public InstrumentGroupValidation()
    {
      InstrumentGroup = null;
      Industry = string.Empty;
      IndustryFound = false;
      Category = string.Empty;
      CategoryFound = false;
      Subcategory = string.Empty;
      SubcategoryFound = false;
      IndustryWords = Array.Empty<string>();
      CategoryWords = Array.Empty<string>();
      SubcategoryWords = Array.Empty<string>();
    }

    public InstrumentAdapter InstrumentAdapter { get; set; }
    public Contract Contract { get; set; }
    public InstrumentGroup? InstrumentGroup { get; set; }
    public string Industry { get; set; }
    public bool IndustryFound { get; set; }
    public string Category { get; set; }
    public bool CategoryFound { get; set; }
    public string Subcategory { get; set; }
    public bool SubcategoryFound { get; set; }
    public string[] IndustryWords { get; set; }
    public string[] CategoryWords { get; set; }
    public string[] SubcategoryWords { get; set; }
    private bool m_initWordLists = false;

    private string[] splitWords(string text)
    {
      return text.Split(WordSeparators, StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Performs a match on the given instrument group and returns a weighted score based on the number of words that match
    /// between the contracts' industry, category and sub-category and the given IndustryGroup.
    /// </summary>
    public Tuple<double, MatchesOn, string> Match(InstrumentGroup instrumentGroup, Guid rootId)
    {
      Tuple<double, MatchesOn, string> result = new Tuple<double, MatchesOn, string>(0.0, MatchesOn.None, string.Empty);
      double highestScore = 0.0;
      if (!m_initWordLists)
      {
        IndustryWords = splitWords(Industry);
        CategoryWords = splitWords(Category);
        SubcategoryWords = splitWords(Subcategory);
        m_initWordLists = true;
      }

      //we compare terminal groups only against the sub-categories defined
      bool terminalGroup = InstrumentAdapter.m_instrumentGroupService.Items.FirstOrDefault((g) => g.ParentId == instrumentGroup.Id) == null;

      string[] nameWords = splitWords(instrumentGroup.Name);
      double score = 0.0;
      
      if (!IndustryFound && !terminalGroup)
      {
        score = matchWords(IndustryWords, nameWords); //industry can only match entries right under the root
        if (score > highestScore)
        {
          result = new Tuple<double, MatchesOn, string>(score, MatchesOn.Industry, $"Matched {pathToInstrumentGroup(instrumentGroup, rootId)} with INDUSTRY \"{Industry}\"");
          highestScore = score;
        }
      }

      if (!CategoryFound && !terminalGroup)
      {
        score = matchWords(CategoryWords, nameWords);
        if (score > highestScore)
        {
          result = new Tuple<double, MatchesOn, string>(score, MatchesOn.Category, $"Matched {pathToInstrumentGroup(instrumentGroup, rootId)} with CATEGORY \"{Industry}\"->\"{Category}\"");
          highestScore = score;
        }
      }

      if (!SubcategoryFound && terminalGroup)
      {
        score = matchWords(SubcategoryWords, nameWords);
        if (score > highestScore)
        {
          result = new Tuple<double, MatchesOn, string>(score, MatchesOn.Subcategory, $"Matched {pathToInstrumentGroup(instrumentGroup, rootId)} with SUB-CATEGORY \"{Industry}\"->\"{Category}\"->\"{Subcategory}\"");
          highestScore = score;
        }
      }

      foreach (var alternateName in instrumentGroup.AlternateNames)
      {
        nameWords = splitWords(alternateName);
        
        if (!IndustryFound && !terminalGroup)
        {
          score = matchWords(IndustryWords, nameWords);
          if (score > highestScore)
          {
            result = new Tuple<double, MatchesOn, string>(score, MatchesOn.Industry, $"Matched {pathToInstrumentGroup(instrumentGroup, rootId, alternateName)} with INDUSTRY \"{Industry}\"");
            highestScore = score;
          }
        }

        if (!CategoryFound && !terminalGroup)
        {
          score = matchWords(CategoryWords, nameWords);
          if (score > highestScore)
          {
            result = new Tuple<double, MatchesOn, string>(score, MatchesOn.Category, $"Matched {pathToInstrumentGroup(instrumentGroup, rootId, alternateName)} with CATEGORY \"{Industry}\"->\"{Category}\"");
            highestScore = score;
          }
        }

        if (!SubcategoryFound && terminalGroup)
        {
          score = matchWords(SubcategoryWords, nameWords);
          if (score > highestScore)
          {
            result = new Tuple<double, MatchesOn, string>(score, MatchesOn.Subcategory, $"Matched {pathToInstrumentGroup(instrumentGroup, rootId, alternateName)} with SUB-CATEGORY \"{Industry}\"->\"{Category}\"->\"{Subcategory}\"");
            highestScore = score;
          }
        }
      }

      return result;
    }

    private double matchWords(string[] words1, string[] words2)
    {
      double score = 0.0;
      int count = words1.Length + words2.Length;

      foreach (var word1 in words1)
        foreach (var word2 in words2)
          if (word1.Equals(word2, StringComparison.OrdinalIgnoreCase))
          {
            score += 1.0;
            break;
          }

      return count > 0 ? score / count : 0.0;
    }

    private string pathToInstrumentGroup(InstrumentGroup instrumentGroup, Guid rootId, string alternateName = "")
    {
      string result;
      if (alternateName != "")
        result = $"\"{instrumentGroup.Name}:{alternateName}\"";
      else
        result = $"\"{instrumentGroup.Name}\"";
      var currentGroup = instrumentGroup;
      while (currentGroup != null && currentGroup.ParentId != rootId)
      {
        currentGroup = InstrumentAdapter.m_instrumentGroupService.Items.FirstOrDefault(g => g.Id == currentGroup.ParentId);
        if (currentGroup != null) result = $"\"{currentGroup.Name}\"->{result}";
      }

      return result;
    }
  }

  /// <summary>
  /// Validate TradeSharp instrument groups against Interactive Brokers defined industries, categories and sub-categories.
  /// </summary>
  public class ValidateInstrumentGroups
  {
    //constants


    //enums


    //types


    //attributes
    private InstrumentAdapter m_adapter;

    //constructors
    public ValidateInstrumentGroups(InstrumentAdapter adapter)
    {
      m_adapter = adapter;
    }

    //finalizers


    //interface implementations


    //properties


    //methods
    public void Run()
    {
      List<InstrumentGroupValidation> definedContractGroups = new List<InstrumentGroupValidation>();

      IProgressDialog progress = m_adapter.m_dialogService.CreateProgressDialog("Validating Instrument Group Definitions", m_adapter.m_logger);
      progress.StatusMessage = "Accumulating industry class definitions from the InteractiveBrokers contract definitions";
      progress.Progress = 0;
      progress.Minimum = 0;
      progress.Maximum = m_adapter.m_instrumentService.Items.Count;
      progress.ShowAsync();

      List<string> missingInstruments = new List<string>();
      foreach (var instrument in m_adapter.m_instrumentService.Items)
      {
        var contract = m_adapter.m_serviceHost.Cache.GetContract(instrument.Ticker, Constants.DefaultExchange);

        if (contract == null)
          foreach (var altTicker in instrument.AlternateTickers)
          {
            contract = m_adapter.m_serviceHost.Cache.GetContract(altTicker, Constants.DefaultExchange);
            if (contract != null) break;
          }

        if (contract == null)
          missingInstruments.Add($"{instrument.Ticker}");
        else
        {
          //check that instrument group would be correct
          if (contract is ContractStock contractStock)
          {
            var contractGroup = definedContractGroups.FirstOrDefault((g) => g.Industry == contractStock.Industry && g.Category == contractStock.Category && g.Subcategory == contractStock.Subcategory);
            if (contractGroup == null)
            {
              contractGroup = new InstrumentGroupValidation { InstrumentAdapter = m_adapter, Contract = contract, Industry = contractStock.Industry, Category = contractStock.Category, Subcategory = contractStock.Subcategory };
              definedContractGroups.Add(contractGroup);
            }
          }
        }

        progress.Progress++;
        if (progress.CancellationTokenSource.IsCancellationRequested) return;  //exit thread when operation is cancelled
      }

      if (missingInstruments.Count > 0)
        using (progress.BeginScope($"Missing {missingInstruments.Count} contracts - run instrument analysis to correct this"))
          foreach (var missingInstrument in missingInstruments)
          {
            progress.LogWarning($"{missingInstrument}");
            if (progress.CancellationTokenSource.IsCancellationRequested) return;
          }

      List<Tuple<InstrumentGroup, string>> matchedInstrumentGroups = new List<Tuple<InstrumentGroup, string>>();
      List<InstrumentGroup> missingInstrumentGroups = new List<InstrumentGroup>();

      //determine non-IB industry groups to validate
      InstrumentGroup? rootGroup = m_adapter.m_instrumentGroupService.Items.FirstOrDefault((g) => g.Name == Constants.DefaultRootInstrumentGroupName && g.Tag == Constants.DefaultRootInstrumentGroupTag);

      List<InstrumentGroup> groupsToValidate;
      if (rootGroup != null)
        //only select groups that are not children of the IB root group since the IB groups will always match and
        //hide problematic non-IB groups
        groupsToValidate = m_adapter.m_instrumentGroupService.Items.Where((g) => !IsChildOf(rootGroup, g)).ToList();
      else
        groupsToValidate = m_adapter.m_instrumentGroupService.Items.ToList();

      if (groupsToValidate.Count == 0)
      {
        progress.LogWarning("No instrument groups to validate.");
        progress.Complete = true;
        return;
      }

      progress.StatusMessage = "Analyzing instrument group definitions";
      progress.Maximum += groupsToValidate.Count;
      foreach (var instrumentGroup in groupsToValidate)
      {
        var contractGroup = definedContractGroups.FirstOrDefault((g) => (!g.IndustryFound && instrumentGroup.Equals(g.Industry)) || (!g.CategoryFound && instrumentGroup.Equals(g.Category)) || (!g.SubcategoryFound && instrumentGroup.Equals(g.Subcategory)));

        if (contractGroup != null)
        {
          if (instrumentGroup.Equals(contractGroup.Industry))
          {
            contractGroup.IndustryFound = true;
            matchedInstrumentGroups.Add(new(instrumentGroup, $"\"{instrumentGroup.Name}\" matched with industry \"{contractGroup.Industry}\""));
          }

          if (instrumentGroup.Equals(contractGroup.Category))
          {
            contractGroup.CategoryFound = true;
            matchedInstrumentGroups.Add(new(instrumentGroup, $"\"{instrumentGroup.Name}\" matched with category \"{contractGroup.Category}\""));
          }

          if (instrumentGroup.Equals(contractGroup.Subcategory))
          {
            contractGroup.SubcategoryFound = true;
            matchedInstrumentGroups.Add(new(instrumentGroup, $"\"{instrumentGroup.Name}\" matched with sub-category \"{contractGroup.Subcategory}\""));
          }
        }
        else
          missingInstrumentGroups.Add(instrumentGroup);

        progress.Progress++;
        if (progress.CancellationTokenSource.IsCancellationRequested) return;  //exit thread when operation is cancelled
      }

      progress.LogInformation($"Matched {matchedInstrumentGroups.Count} instrument groups");

      if (missingInstrumentGroups.Count > 0)
      {
        progress.StatusMessage = $"Searching for potential matches on {missingInstrumentGroups.Count} instrument groups";
        progress.Maximum += missingInstrumentGroups.Count;

        InstrumentGroup? rootInstrumentGroup = m_adapter.m_instrumentGroupService.Items.FirstOrDefault(g => g.ParentId == InstrumentGroup.InstrumentGroupRoot);
        foreach (var instrumentGroup in missingInstrumentGroups)
        {
          List<Tuple<double, InstrumentGroupValidation.MatchesOn, InstrumentGroupValidation, InstrumentGroup, string>> matchScores = new List<Tuple<double, InstrumentGroupValidation.MatchesOn, InstrumentGroupValidation, InstrumentGroup, string>>();
          foreach (var contractGroup in definedContractGroups)
          {
            Tuple<double, InstrumentGroupValidation.MatchesOn, string> result = contractGroup.Match(instrumentGroup, rootInstrumentGroup != null ? rootInstrumentGroup.Id : InstrumentGroup.InstrumentGroupRoot);
            if (matchScores.FirstOrDefault((g) => g.Item1 == result.Item1 && g.Item2 == result.Item2 && g.Item4.Id == instrumentGroup.Id) != null) continue;   //skip repeating the same comparisons
            if (result.Item1 > 0.0) 
              matchScores.Add(new Tuple<double, InstrumentGroupValidation.MatchesOn, InstrumentGroupValidation, InstrumentGroup, string>(result.Item1, result.Item2, contractGroup, instrumentGroup, result.Item3));

            progress.Progress++;
            if (progress.CancellationTokenSource.IsCancellationRequested) return;  //exit thread when operation is cancelled
          }

          if (matchScores.Count > 0)
          {
            Common.Utilities.Sort(matchScores, x => x.Item1);

            ILogCorrections? corrections = null;            
            if (matchScores.Count == 1)
            {
              string caption = $"One match found for group \"{instrumentGroup.Name}\" - {matchScores[0].Item5}";
              corrections = progress.LogInformation(caption);
              corrections.Add($"{matchScores[0].Item5} - score {matchScores[0].Item1:F3}", HandleFixMissingInstrumentGroup, matchScores[0]);
            }
            else
            {
              corrections = progress.LogWarning($"{matchScores.Count} matches found for group \"{instrumentGroup.Name}\"");
              foreach (var match in matchScores)
                  corrections.Add($"{match.Item5} - score {match.Item1:F3}", HandleFixMissingInstrumentGroup, match);
            }
          }
          else
            progress.LogWarning($"No matches found for group \"{instrumentGroup.Name}\".");

          if (progress.CancellationTokenSource.IsCancellationRequested) return;  //exit thread when operation is cancelled
        }

        progress.Progress++;
        if (progress.CancellationTokenSource.IsCancellationRequested) return;  //exit thread when operation is cancelled
      }

      progress.Progress = progress.Maximum;
      progress.Complete = true;
    }

    public void HandleFixMissingInstrumentGroup(object? parameter)
    {
      if (parameter == null)
      {
        m_adapter.m_logger.LogError($"HandleMissingInstrumentGroup encountered null parameter.");
        return;
      }

      if (parameter is Tuple<double, InstrumentGroupValidation.MatchesOn, InstrumentGroupValidation, InstrumentGroup, Guid, string> match)
      {
        string valueToAdd;
        switch (match.Item2)
        {
          case InstrumentGroupValidation.MatchesOn.Industry:
            valueToAdd = match.Item3.Industry;
            break;
          case InstrumentGroupValidation.MatchesOn.Category:
            valueToAdd = match.Item3.Category;
            break;
          case InstrumentGroupValidation.MatchesOn.Subcategory:
            valueToAdd = match.Item3.Subcategory;
            break;
          default:
            m_adapter.m_logger.LogError($"HandleMissingInstrumentGroup encountered incorrect match state.");
            return;
        }

        match.Item4.AlternateNames.Add(valueToAdd);
        m_adapter.m_database.UpdateInstrumentGroup(match.Item4);
        m_adapter.m_logger.LogInformation($"Added alternate name \"{valueToAdd}\" to instrument group \"{match.Item4.Name}\"");
      }
    }

    public bool IsChildOf(InstrumentGroup parent, InstrumentGroup child)
    {
      if (parent == child) return false; //can never be child of self
      if (child.ParentId == InstrumentGroup.InstrumentGroupRoot) return false;  //top-level nodes are never a child of anything

      InstrumentGroup currentNode = child;
      while (currentNode.ParentId != InstrumentGroup.InstrumentGroupRoot)
      {
        if (currentNode.ParentId == parent.Id) return true;
        currentNode = m_adapter.m_instrumentGroupService.Items.FirstOrDefault((g) => g.Id == currentNode.ParentId)!; //NOTE: We allow a crash if the parent is not found since there might be a problem in the tree.
      }

      return false;
    }
  }
}
