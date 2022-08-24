using System;

public class Node
{
    public class NodeItem
    {
        public string Name = "";
    }
    public class NodeFile
    {
        public Node MainNode;
        public List<Node> AllNodes = new List<Node>();
        public List<Variable> Allvariables = new List<Variable>();
        public bool ReadOnly = false;
        public string FileName = "";
        string Path = "";
        public NodeFile()
        {
            MainNode = new Node("__MainNode");
        }
        public NodeFile(string file, bool readonl = false)
        {
            ReadFile(file);
            ReadOnly = readonl;
            Path = file;
        }
        public void ReadFile(string path)
        {
            if (!File.Exists(path))
                return;
            Path = path;
            FileName = path.Split('\\').Last().Replace(".txt", "");
            Node CurrentNode = new Node("__MainNode");
            MainNode = CurrentNode;
            StreamReader Reader = new StreamReader(path);
            string read = "";
            string name = "";
            string value = "";
            bool comment = false;
            bool commentLine = false;
            NodeItem lastObj = null;
            string commentText = "";
            while (!Reader.EndOfStream)
            {
                char character = (char)Reader.Read();

                if (character == '=' && !comment)
                {
                    name = read.Trim();
                    read = "";
                }
                else if (character == '{' && !comment)
                {
                    Node newNode = new Node(name, CurrentNode);
                    CurrentNode.Nodes.Add(newNode);
                    CurrentNode = newNode;
                    lastObj = null;
                    name = "";
                    value = "";
                    read = "";

                }
                else if (character == '}' && !comment)
                {
                    if (!read.Contains('=') && name == "" && value == "")
                    {
                        foreach (string v in read.Replace("\t", " ").Split(' '))
                        {
                            if (v != "" && v != " " && !string.IsNullOrWhiteSpace(v))
                            {
                                CurrentNode.PureValues.Add(v.Trim());
                            }
                        }
                    }
                    lastObj = CurrentNode;
                    CurrentNode.Parent.ItemOrder.Add(CurrentNode);
                    CurrentNode = CurrentNode.Parent;
                    name = "";
                    value = "";
                    read = "";
                }
                else if (character == '\n')
                {
                    if (commentLine)
                    {
                        CurrentNode.Comments.Add(new CommentLine(commentText, lastObj));
                    }
                    else
                    {
                        if (name != "")
                        {
                            Variable v = new Variable(name, read.Trim());
                            v.Comment = commentText;
                            CurrentNode.Variables.Add(v);
                            CurrentNode.ItemOrder.Add(v);
                            lastObj = v;
                        }
                        else
                        {
                            foreach (string v in read.Replace("\t", " ").Split(' '))
                            {
                                if (v != "" && v != " " && !string.IsNullOrWhiteSpace(v))
                                {
                                    CurrentNode.PureValues.Add(v.Trim());
                                }
                            }
                        }
                    }
                    name = "";
                    value = "";
                    read = "";

                    comment = false;
                    commentLine = false;
                    commentText = "";
                }
                else if (character == '#')
                {
                    comment = true;
                    if (read.Trim() == "" || name.Trim() == "")
                    {
                        commentLine = true;
                    }
                }
                else if (comment)
                {
                    commentText += character;
                }
                else
                {
                    read += character;
                }
            }
            if (name != "")
            {
                if (name.Contains(" "))
                {
                    foreach (string v in name.Split(' '))
                    {
                        CurrentNode.Variables.Add(new Variable(v, ""));
                    }
                }
                else
                {
                    CurrentNode.Variables.Add(new Variable(name, read.Trim()));
                }
            }
            Reader.Close();

        }
        public void SaveFile(string path)
        {
            File.WriteAllText(path, Node.NodeToText(MainNode));
        }
    }
    public class CommentLine
    {
        public string Text = "";
        public NodeItem Below = null;
        public CommentLine(string text, NodeItem below)
        {
            Text = text;
            Below = below;
        }
    }
    public class Node : NodeItem
    {
        public string PureInnerText = "";
        public bool UseInnerText = false;
        public Node Parent = null;
        public List<Node> Nodes = new List<Node>();
        public List<Variable> Variables = new List<Variable>();
        public List<string> PureValues = new List<string>();
        public List<CommentLine> Comments = new List<CommentLine>();
        public List<NodeItem> ItemOrder = new List<NodeItem>();
        public Node(string name)
        {
            Name = name;
        }
        public Node(string name, Node parent)
        {
            Name = name;
            Parent = parent;
        }
        public static string NodeToText(Node n)
        {
            string text = "";
            foreach (CommentLine cl in n.Comments)
            {
                if (cl.Below == null)
                    text += "#" + cl.Text + "\n";
            }

            if (n.PureValues.Any())
            {
                int count = 0;
                foreach (string s in n.PureValues)
                {
                    count++;
                    if (count == 10)
                    {
                        count = 0;
                        text += s + "\n";
                    }
                    else
                    {
                        text += s + " ";
                    }
                }
            }
            else
            {
                foreach (NodeItem ni in n.ItemOrder)
                {
                    if (ni is Variable)
                    {
                        Variable v = (Variable)ni;
                        text += v.Name + " = " + v.Value;
                        if (v.Comment != "")
                            text += "#" + v.Comment;
                        text += "\n";
                        foreach (CommentLine cl in n.Comments)
                        {
                            if (cl.Below == v)
                                text += "#" + cl.Text + "\n";
                        }
                    }
                    else if (ni is Node)
                    {
                        Node inner = (Node)ni;
                        text += inner.Name + " = {";
                        if (inner.UseInnerText && false)
                        {
                            text += " " + inner.PureInnerText + " ";
                        }
                        else
                        {
                            text += "\n";
                            string innertext = NodeToText(inner);
                            string tabbedtext = "";
                            foreach (string line in innertext.Split('\n'))
                            {
                                if (line != "")
                                {
                                    tabbedtext += "\t" + line + "\n";
                                }
                            }
                            text += tabbedtext;
                        }
                        text += "}\n";
                        foreach (CommentLine cl in n.Comments)
                        {
                            if (cl.Below == inner)
                                text += "#" + cl.Text + "\n";
                        }
                    }
                }
            }
            return text;
        }
        public bool ChangeVariable(string name, string value, bool forceadd = false)
        {
            Variable v = Variables.Find(x => x.Name == name);
            if (v != null)
            {
                v.Value = value;
                return true;
            }
            else
            {
                if (forceadd)
                {
                    v = new Variable(name, value);
                    Variables.Add(v);
                    ItemOrder.Add(v);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        public Node AddNode(string name)
        {
            Node n = new Node(name, this);
            Nodes.Add(n);
            ItemOrder.Add(n);
            return n;
        }
    }
    public class Variable : NodeItem
    {
        public string Value = "";
        public string Comment = "";
        public Variable(string name, string value)
        {
            Name = name;
            Value = value;
        }
    }
}
