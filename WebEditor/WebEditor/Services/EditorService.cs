﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using AxeSoftware.Quest;

namespace WebEditor.Services
{
    public class EditorService
    {
        private class TreeItem
        {
            public string Key { get; set; }
            public string Text { get; set; }
            public TreeItem Parent { get; set; }
        }

        private EditorController m_controller;
        private Dictionary<string, TreeItem> m_elements = new Dictionary<string, TreeItem>();
        private int m_id;

        public EditorService()
        {
            m_controller = new EditorController();
        }

        public void Initialise(int id, string filename, string libFolder)
        {
            m_id = id;
            if (m_controller.Initialise(filename, libFolder))
            {
                m_controller.ClearTree += new EditorController.VoidHandler(m_controller_ClearTree);
                m_controller.BeginTreeUpdate += new EditorController.VoidHandler(m_controller_BeginTreeUpdate);
                m_controller.AddedNode += new EditorController.AddedNodeHandler(m_controller_AddedNode);
                m_controller.EndTreeUpdate += new EditorController.VoidHandler(m_controller_EndTreeUpdate);
                m_controller.UpdateTree();
            }
        }

        public EditorController Controller { get { return m_controller; } }

        void m_controller_AddedNode(string key, string text, string parent, bool isLibraryNode, int? position)
        {
            m_elements.Add(key, new TreeItem
            {
                Key = key,
                Text = text,
                Parent = (parent == null) ? null : m_elements[parent]
            });
        }

        void m_controller_ClearTree()
        {
            m_elements.Clear();
        }

        void m_controller_BeginTreeUpdate()
        {
            m_elements.Clear();
        }

        void m_controller_EndTreeUpdate()
        {
        }

        private class JsonTreeElement
        {
            public Dictionary<string, string> attr = new Dictionary<string, string>();
            public string data;
            //public string state;
            public IEnumerable<JsonTreeElement> children;
        }

        private class JsonParentElement
        {
            public IEnumerable<JsonTreeElement> data;
        }

        public object GetElementTreeForJson()
        {
            return new JsonParentElement { data = GetJsonTreeItemsForParent(null) };
        }

        private List<JsonTreeElement> GetJsonTreeItemsForParent(string parent)
        {
            List<JsonTreeElement> result = new List<JsonTreeElement>();
            TreeItem parentElement = (parent == null) ? null : m_elements[parent];
            foreach (TreeItem item in m_elements.Values.Where(e => e.Parent == parentElement))
            {
                JsonTreeElement modelTreeItem = new JsonTreeElement
                {
                    data = item.Text,
                    children = GetJsonTreeItemsForParent(item.Key)
                };
                modelTreeItem.attr.Add("data-key", item.Key);
                result.Add(modelTreeItem);
            }
            return result;
        }

        public Models.Element GetElementModelForView(int gameId, string key, string tab)
        {
            IEditorData data = m_controller.GetEditorData(key);
            IEditorDefinition def = m_controller.GetEditorDefinition(m_controller.GetElementEditorName(key));
            return new Models.Element
            {
                GameId = gameId,
                Key = key,
                Name = m_controller.GetDisplayName(key),
                EditorData = data,
                EditorDefinition = def,
                Tab = tab
            };
        }

        public void SaveElement(string key, Models.ElementSaveData saveData)
        {
            IEditorData data = m_controller.GetEditorData(key);
            foreach (var kvp in saveData.Values)
            {
                object currentValue = data.GetAttribute(kvp.Key);
                IEditableScripts script = currentValue as IEditableScripts;
                if (script != null)
                {
                    SaveScript(script, kvp.Value as WebEditor.Models.ElementSaveData.ScriptsSaveData);
                }
                else
                {
                    if (DataChanged(currentValue, (kvp.Value)))
                    {
                        System.Diagnostics.Debug.WriteLine("New value for {0}: Was {1}, now {2}", kvp.Key, data.GetAttribute(kvp.Key), kvp.Value);
                        data.SetAttribute(kvp.Key, kvp.Value);
                    }
                }
            }
            if (!string.IsNullOrEmpty(saveData.AdditionalAction))
            {
                ProcessAdditionalAction(key, saveData.AdditionalAction);
            }
        }

        private bool DataChanged(object oldValue, object newValue)
        {
            if (oldValue == null && newValue == null)
            {
                return false;
            }
            if (oldValue == null && newValue is string)
            {
                return ((string)newValue).Length > 0;
            }
            if (oldValue == null && newValue is bool)
            {
                return (bool)newValue;
            }
            if (oldValue is string && newValue is string)
            {
                return (string)oldValue != (string)newValue;
            }
            if (oldValue is bool && newValue is bool)
            {
                return (bool)oldValue != (bool)newValue;
            }
            throw new NotImplementedException();
        }

        private void SaveScript(IEditableScripts scripts, WebEditor.Models.ElementSaveData.ScriptsSaveData saveData)
        {
            int count = 0;
            foreach (IEditableScript script in scripts.Scripts)
            {
                WebEditor.Models.ElementSaveData.ScriptSaveData data = saveData.ScriptLines[count];

                if (script.Type != ScriptType.If)
                {
                    foreach (var attribute in data.Attributes)
                    {
                        object oldValue = script.GetParameter(attribute.Key);
                        object newValue = attribute.Value;
                        if (DataChanged(oldValue, newValue))
                        {
                            System.Diagnostics.Debug.WriteLine("New value for script: Was {0}, now {1}", oldValue, newValue);
                            script.SetParameter(attribute.Key, newValue);
                        }
                    }
                }
                else
                {
                    EditableIfScript ifScript = (EditableIfScript)script;
                    object oldExpression = ifScript.GetAttribute("expression");
                    object newExpression = data.Attributes["expression"];
                    if (DataChanged(oldExpression, newExpression))
                    {
                        ifScript.SetAttribute("expression", newExpression);
                    }

                    SaveScript(ifScript.ThenScript, (WebEditor.Models.ElementSaveData.ScriptsSaveData)data.Attributes["then"]);

                    int elseIfCount = 0;
                    foreach (EditableIfScript.EditableElseIf elseIfScript in ifScript.ElseIfScripts)
                    {
                        object oldElseIfExpression = elseIfScript.GetAttribute("expression");
                        object newElseIfExpression = data.Attributes[string.Format("elseif{0}-expression", elseIfCount)];
                        if (DataChanged(oldElseIfExpression, newElseIfExpression))
                        {
                            elseIfScript.SetAttribute("expression", newElseIfExpression);
                        }

                        SaveScript(elseIfScript.EditableScripts, (WebEditor.Models.ElementSaveData.ScriptsSaveData)data.Attributes[string.Format("elseif{0}-then", elseIfCount)]);
                        elseIfCount++;
                    }

                    if (ifScript.ElseScript != null)
                    {
                        SaveScript(ifScript.ElseScript, (WebEditor.Models.ElementSaveData.ScriptsSaveData)data.Attributes["else"]);
                    }
                }

                count++;
            }
        }

        public Models.StringList GetStringList(string key, IEditorControl ctl)
        {
            IEditableList<string> value = (IEditableList<string>)m_controller.GetEditorData(key).GetAttribute(ctl.Attribute);

            IDictionary<string, string> items = null;
            if (value != null)
            {
                items = new Dictionary<string, string>();
                foreach (var item in value.Items)
                {
                    items.Add(item.Key, item.Value.Value);
                }
            }

            return new Models.StringList
            {
                Attribute = ctl.Attribute,
                EditPrompt = ctl.GetString("editprompt"),
                Items = items
            };
        }

        public Models.Script GetScript(string key, IEditorControl ctl)
        {
            IEditableScripts value = (IEditableScripts)m_controller.GetEditorData(key).GetAttribute(ctl.Attribute);

            return new Models.Script
            {
                Attribute = ctl.Attribute,
                Controller = m_controller,
                Scripts = value
            };
        }

        private void ProcessAdditionalAction(string key, string action)
        {
            string[] data = action.Split(new[] { ' ' }, 2);
            string cmd = data[0];
            string parameter = data[1];
            switch (cmd)
            {
                case "stringlist":
                    data = parameter.Split(new[] { ' ' }, 2);
                    string stringListCmd = data[0];
                    string stringListParameter = data[1];
                    switch (stringListCmd)
                    {
                        case "add":
                            data = stringListParameter.Split(new[] { ';' }, 2);
                            StringListAdd(key, data[0], data[1]);
                            break;
                        case "edit":
                            data = stringListParameter.Split(new[] { ';' }, 3);
                            StringListEdit(key, data[0], data[1], data[2]);
                            break;
                        case "delete":
                            data = stringListParameter.Split(new[] { ';' }, 2);
                            StringListDelete(key, data[0], data[1]);
                            break;
                    }
                    break;
                case "script":
                    data = parameter.Split(new[] { ' ' }, 2);
                    string scriptCmd = data[0];
                    string scriptParameter = data[1];
                    switch (scriptCmd)
                    {
                        case "add":
                            data = scriptParameter.Split(new[] { ';' }, 2);
                            ScriptAdd(key, data[0], data[1]);
                            break;
                        case "delete":
                            data = scriptParameter.Split(new[] { ';' }, 2);
                            if (data[1].Length > 0)
                            {
                                ScriptDelete(key, data[0], data[1].Split(';'));
                            }
                            break;
                        case "addelse":
                            ScriptAddElse(key, scriptParameter);
                            break;
                        case "addelseif":
                            ScriptAddElseIf(key, scriptParameter);
                            break;
                        case "deleteifsection":
                            data = scriptParameter.Split(new[] { ';' }, 2);
                            if (data[1].Length > 0)
                            {
                                ScriptDeleteIfSection(key, data[0], data[1].Split(';'));
                            }
                            break;
                    }
                    break;
            }
        }

        private void StringListAdd(string element, string attribute, string value)
        {
            // TO DO: if (m_data.ReadOnly) return;
            // TO DO: Validate input first

            IEditableList<string> list = m_controller.GetEditorData(element).GetAttribute(attribute) as IEditableList<string>;
            if (list == null)
            {
                list = m_controller.CreateNewEditableList(element, attribute, value, true);
            }
            else
            {
                PrepareStringListForEditing(element, attribute, ref list);
                list.Add(value);
            }
        }

        private void StringListEdit(string element, string attribute, string key, string value)
        {
            // TO DO: if (m_data.ReadOnly) return;
            // TO DO: Validate input first

            IEditableList<string> list = m_controller.GetEditorData(element).GetAttribute(attribute) as IEditableList<string>;
            PrepareStringListForEditing(element, attribute, ref list);
            list.Update(key, value);
        }

        private void StringListDelete(string element, string attribute, string key)
        {
            // TO DO: if (m_data.ReadOnly) return;

            IEditableList<string> list = m_controller.GetEditorData(element).GetAttribute(attribute) as IEditableList<string>;
            PrepareStringListForEditing(element, attribute, ref list);
            list.Remove(key);
        }

        private void PrepareStringListForEditing(string element, string attribute, ref IEditableList<string> list)
        {
            // If we're currently displaying a list which belongs to a type we inherit from,
            // we must clone the list before we can edit it.

            if (list == null) return;
            if (list.Owner != element)
            {
                list = list.Clone(element, attribute);
            }
        }

        private void ScriptAdd(string element, string attribute, string value)
        {
            // TO DO: if (m_data.ReadOnly) return;

            IEditableScripts script = GetScript(element, attribute);

            // TO DO: For child scripts, use something like this:
            // m_scripts = m_controller.CreateNewEditableScriptsChild(m_parentScript, m_helper.ControlDefinition.Attribute, script, true);

            if (script == null)
            {
                m_controller.CreateNewEditableScripts(element, attribute, value, true, true);
            }
            else
            {
                script.AddNew(value, element);
            }
        }

        private void ScriptDelete(string element, string attribute, string[] indexes)
        {
            // TO DO: if (m_data.ReadOnly) return;

            IEditableScripts script = GetScript(element, attribute);
            script.Remove(indexes.Select(i => int.Parse(i)).ToArray());
        }

        private void ScriptAddElse(string element, string attribute)
        {
            // TO DO: if (m_data.ReadOnly) return;

            IEditableScript scriptLine = GetScriptLine(element, attribute);
            EditableIfScript ifScript = (EditableIfScript)scriptLine;
            ifScript.AddElse();
        }

        private void ScriptAddElseIf(string element, string attribute)
        {
            // TO DO: if (m_data.ReadOnly) return;

            IEditableScript scriptLine = GetScriptLine(element, attribute);
            EditableIfScript ifScript = (EditableIfScript)scriptLine;
            ifScript.AddElseIf();
        }

        private void ScriptDeleteIfSection(string element, string attribute, string[] sections)
        {
            // TO DO: if (m_data.ReadOnly) return;

            IEditableScript scriptLine = GetScriptLine(element, attribute);
            EditableIfScript ifScript = (EditableIfScript)scriptLine;

            List<EditableIfScript.EditableElseIf> elseIfsToRemove = new List<EditableIfScript.EditableElseIf>();

            foreach (string section in sections)
            {
                if (section == "else")
                {
                    ifScript.RemoveElse();
                }
                else if(section.StartsWith("elseif"))
                {
                    elseIfsToRemove.Add(ifScript.ElseIfScripts.ElementAt(int.Parse(section.Substring(6))));
                }
            }

            foreach (EditableIfScript.EditableElseIf elseIfToRemove in elseIfsToRemove)
            {
                ifScript.RemoveElseIf(elseIfToRemove);
            }
        }

        private IEditableScripts GetScript(string element, string attribute)
        {
            IEditableScripts script;

            if (attribute.IndexOf("-") == -1)
            {
                script = m_controller.GetEditorData(element).GetAttribute(attribute) as IEditableScripts;
            }
            else
            {
                string parameter;
                IEditableScript scriptLine = GetScriptLine(element, attribute, out parameter);
                if (parameter == "then")
                {
                    EditableIfScript ifScript = (EditableIfScript)scriptLine;
                    script = ifScript.ThenScript;
                }
                else if (parameter == "else")
                {
                    EditableIfScript ifScript = (EditableIfScript)scriptLine;
                    script = ifScript.ElseScript;
                }
                else if (parameter.StartsWith("elseif"))
                {
                    int elseIfIndex = int.Parse(parameter.Substring(6));
                    EditableIfScript ifScript = (EditableIfScript)scriptLine;
                    script = ifScript.ElseIfScripts.ElementAt(elseIfIndex).EditableScripts;
                }
                else
                {
                    // TO DO: Handle "script-2-then-3-elseif4-then" etc...
                    throw new NotImplementedException();
                }
            }

            return script;
        }

        private IEditableScript GetScriptLine(string element, string attribute)
        {
            string parameter;
            return GetScriptLine(element, attribute, out parameter);
        }

        private IEditableScript GetScriptLine(string element, string attribute, out string parameter)
        {
            string[] path = attribute.Split(new[] { '-' }, 4);
            IEditableScripts parent = m_controller.GetEditorData(element).GetAttribute(path[0]) as IEditableScripts;
            IEditableScript scriptLine = parent.Scripts.ElementAt(int.Parse(path[1]));
            if (path.Length == 2)
            {
                parameter = null;
            }
            else
            {
                parameter = path[2];
            }
            return scriptLine;
        }

        private class ScriptAdderCategory
        {
            public List<ScriptAdderItem> items = new List<ScriptAdderItem>();
        }

        private class ScriptAdderItem
        {
            public string display;
            public string create;
        }

        public object GetScriptAdderJson()
        {
            Dictionary<string, ScriptAdderCategory> categories = new Dictionary<string, ScriptAdderCategory>();
            foreach (string cat in m_controller.GetAllScriptEditorCategories())
            {
                categories.Add(cat, new ScriptAdderCategory());
            }

            foreach (EditableScriptData data in m_controller.GetScriptEditorData().Values)
            {
                categories[data.Category].items.Add(new ScriptAdderItem
                {
                    display = data.AdderDisplayString,
                    create = data.CreateString
                });
            }

            return categories;
        }
    }
}