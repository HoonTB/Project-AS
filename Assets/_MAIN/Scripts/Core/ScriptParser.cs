using System.Collections.Generic;
using System.Text.RegularExpressions;

public class ScriptParser
{
    private static readonly Regex TagRegex = new Regex(@"^\[(\w+)(?:\s+(.*))?\]$");
    private static readonly Regex AttrRegex = new Regex(@"(\w+)=(""[^""]*""|'[^']*'|[^ \t\]]+)");
    private static readonly Regex ChoiceOptionRegex = new Regex(@"^\*\s*(.+?)\s*>\s*(.+)$");

    public static Script Parse(string text)
    {
        List<ScriptAction> actions = new();
        Dictionary<string, int> sceneMap = new();

        ScriptAction lastChoice = null;

        text = Regex.Replace(text, "<shake>", "<link=shake>");
        text = Regex.Replace(text, "</shake>", "</link>");
        text = Regex.Replace(text, "\r\n", "\n");

        string[] lines = text.Split("\n");

        foreach (var rawLine in lines)
        {
            string line = rawLine.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                continue;

            Match tagMatch = TagRegex.Match(line);
            if (tagMatch.Success)
            {
                string tagName = tagMatch.Groups[1].Value;
                string attrString = tagMatch.Groups[2].Value;

                var scriptAction = new ScriptAction { Type = tagName };
                ParseAttributes(attrString, scriptAction.Params);

                if (tagName == "scene")
                {
                    string sceneName = scriptAction.GetParam("name");
                    if (!string.IsNullOrEmpty(sceneName) && !sceneMap.ContainsKey(sceneName))
                    {
                        sceneMap[sceneName] = actions.Count;
                    }
                }

                else if (tagName == "choices")
                {
                    scriptAction.Choices = new List<Dictionary<string, string>>();
                    lastChoice = scriptAction;
                }

                actions.Add(scriptAction);
                continue;
            }

            Match choiceMatch = ChoiceOptionRegex.Match(line);
            if (choiceMatch.Success && lastChoice != null)
            {
                lastChoice.Choices.Add(
                    new Dictionary<string, string>
                    {
                        { "type", "msg" },
                        { "content", choiceMatch.Groups[1].Value.Trim() },
                        { "goto", choiceMatch.Groups[2].Value.Trim() },
                    }
                );
                continue;
            }

            actions.Add(new ScriptAction { Type = "msg", Params = { { "content", line } } });
        }

        return new Script(actions, sceneMap);
    }

    private static void ParseAttributes(string attrString, Dictionary<string, object> paramDict)
    {
        if (string.IsNullOrWhiteSpace(attrString))
            return;

        foreach (Match m in AttrRegex.Matches(attrString))
        {
            string key = m.Groups[1].Value;
            string rawValue = m.Groups[2].Value;

            if (rawValue.Length >= 2 && (rawValue.StartsWith("\"") || rawValue.StartsWith("'")))
                rawValue = rawValue.Substring(1, rawValue.Length - 2);

            paramDict[key] = rawValue;
        }
    }
}
