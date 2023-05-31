using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;

// From DEngine.Data.Yaml
namespace SimpleGui
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    internal class YamlIgnoreAttribute : Attribute
    {
    }

    internal class TinyYamlNode
    {
        public string Name;
        public string Value;
        public string Comment;
        
        public List<TinyYamlNode> Nodes = new List<TinyYamlNode>();
    }

    internal static class TinyYaml
    {
        private static Assembly DataAssembly = Assembly.Load("SimpleGui");

        // Convert an object to TinyYaml string
        public static string ToYaml<T>(this T obj)
        {
            List<TinyYamlNode> nodes = ObjectToNodes(obj);
            return NodesToYaml(nodes);
        }

        // Write an object to TinyYaml file
        public static void ToYamlFile<T>(this T obj, string filename)
        {
            string yaml = ToYaml(obj);

            if (string.IsNullOrWhiteSpace(yaml))
                return;

            File.WriteAllText(filename, yaml);
        }
    
        // Read a Yaml file and create an object
        public static T FromYamlFile<T>(string filename) where T : new()
        {
            List<TinyYamlNode> nodes = FileToNodes(filename);
            T result = new T();
            Type type = typeof(T);

            if (IsDictionary(type))
            {
                return (T)NodesToDictionary(nodes, type.GenericTypeArguments[0], type.GenericTypeArguments[1]);
            }

            if (IsList(type))
            {
                return (T)NodesToList(nodes, type.GenericTypeArguments[0]);
            }

            PopulateObject(result, nodes);
            return result;
        }

        // Read a Yaml file and create an object
        public static IList FromYamlFile(string filename, Type t)
        {
            List<TinyYamlNode> nodes = FileToNodes(filename);
            return NodesToList(nodes, t);
        }

        // Convert a dictionary object into yaml nodes
        private static List<TinyYamlNode> DictionaryToNodes(IDictionary obj)
        {
            IEnumerable<TypeInfo> dataTypes = DataAssembly.DefinedTypes;
            List<TinyYamlNode> list = new List<TinyYamlNode>();
            foreach (DictionaryEntry f in obj)
            {
                if (f.Value == null)
                    continue;
                
                Type type = f.Value.GetType();
                string strVal = TypeToString(f.Value, type, type.BaseType);

                TinyYamlNode node = new TinyYamlNode
                {
                    Name = f.Key.ToString(),
                    Value = strVal,
                    // TODO: Comment
                };
                list.Add(node);

                if (string.IsNullOrWhiteSpace(strVal))
                {
                    // It might be a type from DEngine.Data
                    // TODO: Repeated from ObjectToNodes
                    if (dataTypes.Any(x => x.Name == type.Name))
                    {
                        // It's a custom object, traverse
                        List<TinyYamlNode> childList = ObjectToNodes(f.Value);
                        node.Nodes.AddRange(childList);
                    }
                }

            }

            return list;
        }

        // Convert a list object into yaml nodes
        private static List<TinyYamlNode> ListToNodes(IList obj)
        {
            IEnumerable<TypeInfo> dataTypes = DataAssembly.DefinedTypes;
            List<TinyYamlNode> result = new List<TinyYamlNode>();
            foreach (object f in obj)
            {
                Type type = f.GetType();
                string strVal = TypeToString(f, type, type.BaseType);

                TinyYamlNode node = new TinyYamlNode
                {
                    Name = string.Empty,
                    Value = strVal,
                    // TODO: Comment
                };
                result.Add(node);

                if (string.IsNullOrWhiteSpace(strVal))
                {
                    // It might be a type from DEngine.Data
                    // TODO: Repeated from ObjectToNodes
                    if (dataTypes.Any(x => x.Name == type.Name))
                    {
                        // It's a custom object, traverse
                        List<TinyYamlNode> childList = ObjectToNodes(f);
                        node.Nodes.AddRange(childList);
                    }
                }

            }

            return result;
        }

        // Convert a list of yaml nodes to a dictionary object of the desired types
        private static IDictionary NodesToDictionary(List<TinyYamlNode> list, Type dType1, Type dType2)
        {
            IEnumerable<TypeInfo> dataTypes = DataAssembly.DefinedTypes;
            Type dictType = typeof(Dictionary<,>).MakeGenericType(dType1, dType2);
            IDictionary dict = (IDictionary)Activator.CreateInstance(dictType);
            bool valueIsData = dataTypes.Contains(dType2);
            
            foreach (TinyYamlNode f in list)
            {
                bool nameIsEnum = dType1.BaseType?.Name == "Enum";
                bool valueIsEnum = dType2.BaseType?.Name == "Enum";
                object dataValue = null;
                if (valueIsData)
                {
                    dataValue = Activator.CreateInstance(dType2);
                    PopulateObject(dataValue, f.Nodes);
                }

                object key = nameIsEnum ? Convert.ChangeType(Enum.Parse(dType1, f.Name), dType1) : Convert.ChangeType(f.Name, dType1);
                object value = valueIsEnum ? Convert.ChangeType(Enum.Parse(dType2, f.Value), dType2) :
                        valueIsData ? dataValue : Convert.ChangeType(f.Value, dType2);

                if (key != null)
                    dict?.Add(key, value);
            }

            return dict;
        }

        // Convert a list of yaml nodes to a list object of the desired types
        // Only supports classes
        private static IList NodesToList(List<TinyYamlNode> list, Type dType)
        {
            IEnumerable<TypeInfo> dataTypes = DataAssembly.DefinedTypes;
            Type dictType = typeof(List<>).MakeGenericType(dType);
            IList dict = (IList) Activator.CreateInstance(dictType);
            bool isData = dataTypes.Contains(dType);

            foreach (TinyYamlNode f in list)
            {
                if (isData)
                {
                    object dataValue = Activator.CreateInstance(dType);
                    PopulateObject(dataValue, f.Nodes);
                    dict?.Add(dataValue);
                }
            }

            return dict;
        }

        private static bool IsDictionary(MemberInfo type) { return type.Name.StartsWith("Dictionary") || type.Name.StartsWith("IDictionary"); }

        private static bool IsList(MemberInfo type) { return type.Name.StartsWith("List") || type.Name.StartsWith("IList"); }

        // Convert an object to yaml nodes
        private static List<TinyYamlNode> ObjectToNodes(object obj)
        {
            List<TinyYamlNode> list = new List<TinyYamlNode>();
            Type type = obj.GetType();
            PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            IEnumerable<TypeInfo> dataTypes = DataAssembly.DefinedTypes;

            if (IsDictionary(type))
                return DictionaryToNodes(obj as IDictionary);
            if (IsList(type))
                return ListToNodes(obj as IList);

            void ObjToNodes(Type fieldType, object value, string name)
            {
                Type baseType = fieldType.BaseType;
                string valueStr = TypeToString(value, fieldType, baseType);

                TinyYamlNode node = new TinyYamlNode
                {
                    Name = name, Value = valueStr,
                    // TODO: Comment
                };
                list.Add(node);
                if (string.IsNullOrWhiteSpace(valueStr))
                {
                    if (IsDictionary(fieldType))
                    {
                        List<TinyYamlNode> childList = DictionaryToNodes(value as IDictionary);
                        node.Nodes.AddRange(childList);
                    }
                    else if (IsList(fieldType))
                    {
                        List<TinyYamlNode> childList = ListToNodes(value as IList);
                        node.Nodes.AddRange(childList);
                    }
                    // It might be a type from DEngine.Data
                    else if (dataTypes.Any(x => x.Name == fieldType.Name))
                    {
                        if (value != null)
                        {
                            List<TinyYamlNode> childList = ObjectToNodes(value);
                            node.Nodes.AddRange(childList);
                        }
                    }
                }
            }

            foreach (FieldInfo f in fields)
            {
                if (Attribute.IsDefined(f, typeof(YamlIgnoreAttribute)))
                    continue;

                object value = f.GetValue(obj);
                ObjToNodes(f.FieldType, value, f.Name);
            }
            foreach (PropertyInfo f in properties)
            {
                if (Attribute.IsDefined(f, typeof(YamlIgnoreAttribute)))
                    continue;

                if (!f.CanWrite)
                    continue;

                object value = f.GetValue(obj);
                ObjToNodes(f.PropertyType, value, f.Name);
            }
            return list;
        }

        /// <summary>
        /// Convert a file to yaml nodes.
        /// Parses the file line by line.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        private static List<TinyYamlNode> FileToNodes(string filename)
        {
            TinyYamlNode rootNode = new TinyYamlNode();

            TinyYamlNode LastNodeAtLevel(int desiredLevel)
            {
                int currentLevel = 0;

                if (desiredLevel == currentLevel) return rootNode;

                if (desiredLevel < 0) return null;

                TinyYamlNode lastNode = rootNode;
                while (currentLevel < desiredLevel)
                {
                    if (lastNode.Nodes.Count == 0) return null;

                    lastNode = lastNode.Nodes.Last();
                    currentLevel++;
                }

                return lastNode;
            }


            string[] lines = File.ReadAllLines(filename);
            int lineNum = 0;
            int level = 0;
            foreach (string line in lines)
            {
                ++lineNum;
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                string valBuf = string.Empty;
                string name;
                string value = string.Empty;
                string comment = string.Empty;

                string newLine = line.TrimEnd();
                int indexOfComment = newLine.IndexOf('#');

                if (indexOfComment >= 0)
                {
                    string commentLine = newLine.Substring(indexOfComment).Trim();
                    comment = commentLine.Substring(1).Trim();
                    newLine = newLine.Replace(commentLine, string.Empty).TrimEnd();
                }

                // Get depth
                int tabCount = newLine.Length - newLine.Replace("\t", string.Empty).Length;
                if (tabCount > level + 1)
                    throw new ArgumentException("Error at line " + lineNum + ": Improper indent.");

                level = tabCount;

                newLine = newLine.Trim();


                int indexOfColon = newLine.IndexOf(':');

                //if (indexOfColon == 0)
                //    throw new ArgumentException("Error at line " + lineNum + ": Line can't begin with a ':'.");

                //if (newLine.Length - newLine.Replace(":", "").Length > 1)
                //    throw new ArgumentException("Error at line " + lineNum + ": Too many colons.");

                if (indexOfColon > 0)
                {
                    name = newLine[..indexOfColon].Trim();
                    value = newLine[(indexOfColon + 1)..].Trim();
                }
                else
                {
                    // Name only
                    name = newLine;
                }

                //if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(comment))
                //    throw new ArgumentException("Error at line " + lineNum + ": Both name and comment was missing.");

                TinyYamlNode node = new TinyYamlNode
                {
                    Name = name,
                    Value = value,
                    Comment = comment,
                };
                
                if (tabCount == 0)
                {
                    rootNode.Nodes.Add(node);
                }
                else
                {
                    TinyYamlNode parentNode = LastNodeAtLevel(level);
                    parentNode.Nodes.Add(node);
                }
            }
            
            return rootNode.Nodes;
        }

        /// <summary>
        /// Convert a list of yaml nodes to yaml string output
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        private static string NodesToYaml(List<TinyYamlNode> list)
        {
            List<string> lineList = new List<string>();

            foreach (TinyYamlNode n in list)
            {
                IterateNodesAsYaml(n, 0, ref lineList);
            }

            string result = String.Join("\r\n", lineList);
            return result;
        }

        /// <summary>
        /// Populate an object from yaml nodes
        /// </summary>
        /// <param name="result"></param>
        /// <param name="list"></param>
        private static void PopulateObject(object result, List<TinyYamlNode> list)
        {
            Type type = result.GetType();
            PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            IEnumerable<TypeInfo> dataTypes = DataAssembly.DefinedTypes;

            foreach (TinyYamlNode n in list)
            {
                object PopulateObjectField(Type fieldType)
                {
                    object val = ParseField(n, fieldType);
                    if (val != null)
                    {
                        return val;
                    }

                    if (IsDictionary(fieldType)) // It might be a dictionary type
                    {
                        IDictionary dict = NodesToDictionary(n.Nodes, fieldType.GenericTypeArguments[0], fieldType.GenericTypeArguments[1]);
                        return dict;
                    }
                    if (IsList(fieldType))
                    {
                        IList dict = NodesToList(n.Nodes, fieldType.GenericTypeArguments[0]);
                        return dict;
                    }
                    // It might be a type from DEngine.Data
                    if (dataTypes.Any(x => x.Name == fieldType.Name))
                    {
                        object newObj = Activator.CreateInstance(fieldType);
                        PopulateObject(newObj, n.Nodes);
                        return newObj;
                    }

                    return null;
                }

                PropertyInfo matchingProperty = properties.FirstOrDefault(x => x.Name == n.Name);
                if (matchingProperty != null && matchingProperty.CanWrite)
                {
                    matchingProperty.SetValue(result, PopulateObjectField(matchingProperty.PropertyType));
                }

                FieldInfo matchingField = fields.FirstOrDefault(x => x.Name == n.Name);
                if (matchingField != null)
                {
                    matchingField.SetValue(result, PopulateObjectField(matchingField.FieldType));
                }

                if (matchingField == null && matchingProperty == null)
                    throw new ArgumentException("Couldn't find property or field: " + n.Name);
            }
        }

        private static float[] ParseVectorString(string vecString)
        {
            string strVal = vecString.Replace("<", string.Empty).Replace(">", string.Empty);
            return strVal.Split(new[] { ',' }).Select(x => Convert.ToSingle(x.Trim())).ToArray();
        }

        private static string TypeToString(object val, Type type, Type baseType)
        {
            if (val == null)
                return null;

            if (type == typeof(bool))
            {
                return (bool)val ? "true" : "false";
            }

            if (baseType == typeof(Enum))
            {
                return val.ToString();
            }

            if (type == typeof(string))
            {
                return val.ToString()?.Trim();
            }

            if (type == typeof(int))
            {
                return val.ToString();
            }

            if (type == typeof(float))
            {
                return val.ToString();
            }

            if (type == typeof(double))
            {
                return val.ToString();
            }

            if (type == typeof(long))
            {
                return val.ToString();
            }

            if (type == typeof(Vector2))
            {
                return val.ToString();
            }

            if (type == typeof(Vector3))
            {
                return val.ToString();
            }

            if (type == typeof(Vector4))
            {
                return val.ToString();
            }

            if (type == typeof(Matrix4x4))
            {
                return val.ToString();
            }

            if (type == typeof(string[]))
            {
                return string.Join(", ", (string[])val);
            }

            return type == typeof(int[]) ? string.Join(", ", (int[])val) : null;
        }

        private static object ParseField(TinyYamlNode n, Type type)
        {
            Type baseType = type.BaseType;

            if (type == typeof(string))
            {
                return n.Value.Trim();
            }

            if (baseType == typeof(Enum))
            {
                return Enum.Parse(type, n.Value);
            }

            if (type == typeof(bool))
            {
                return n.Value.ToLowerInvariant() == "true";
            }

            if (type == typeof(int))
            {
                return Convert.ToInt32(n.Value);
            }

            if (type == typeof(long))
            {
                return Convert.ToInt64(n.Value);
            }

            if (type == typeof(float))
            {
                return Convert.ToSingle(n.Value);
            }

            if (type == typeof(double))
            {
                return Convert.ToDouble(n.Value);
            }

            if (type == typeof(Vector2))
            {
                float[] v = ParseVectorString(n.Value);
                return new Vector2(v[0], v[1]);
            }

            if (type == typeof(Vector3))
            {
                float[] v = ParseVectorString(n.Value);
                return new Vector3(v[0], v[1], v[2]);
            }

            if (type == typeof(Vector4))
            {
                float[] v = ParseVectorString(n.Value);
                return new Vector4(v[0], v[1], v[2], v[3]);
            }

            if (type == typeof(List<string>))
            {
                return n.Nodes.Select(x => x.Name[1..].Trim()).ToList();
            }

            if (type == typeof(string[]))
            {
                return n.Value.Split(new[] { ',' }).Select(x => x.Trim()).ToArray();
            }

            if (type == typeof(int[]))
            {
                return n.Value.Split(new[] { ',' }).Select(_ => Convert.ToInt32(n.Value)).ToArray();
            }

            if (type == typeof(float[]))
            {
                return n.Value.Split(new[] { ',' }).Select(_ => Convert.ToSingle(n.Value)).ToArray();
            }

            return null;
        }

        static void IterateNodesAsYaml(TinyYamlNode n, int level, ref List<string> list)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(string.Concat(Enumerable.Repeat("\t", level)));
            sb.Append(n.Name);
            sb.Append(": ");
            sb.Append(n.Value);
            sb.Append(string.IsNullOrWhiteSpace(n.Comment) ? string.Empty : " # " + n.Comment);

            list.Add(sb.ToString());
            foreach (TinyYamlNode child in n.Nodes)
            {
                IterateNodesAsYaml(child, level + 1, ref list);
            }
        }
    }
}
