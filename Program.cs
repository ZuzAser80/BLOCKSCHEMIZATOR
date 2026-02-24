using System.Collections.Generic;
using System.Xml.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using System.ComponentModel;
using System.Data.Common;
using System.Reflection.Metadata;
using System.ComponentModel.Design.Serialization;
using System.Runtime.Serialization;

DrawIoGenerator.CreateSimpleDiagram();

public class ConnectData
{
    public int x, y, x_min = int.MaxValue, x_max = int.MinValue;
    public string connection_start, line;
    //public ConnectType type;
    public ConnectData(int x, int y, string connection_start, string line)
    {
        this.x = x;
        this.y = y;
        this.connection_start = connection_start;
        //this.type = type;
        this.line = line;
    }
    public void update_x(int _x)
    {
        if (x_max < _x)
        {
            x_max = _x;
        }
        if (x_min > _x)
        {
            x_min = _x;
        }
    }
}

public class IfElseData
{
    public string ConditionId { get; set; }
    public string LastTrueId { get; set; }
    public string LastFalseId { get; set; }
    public int X { get; set; }
    public int Y { get; set; }

    public int TrueX {get;set;}
    public int FalseX {get;set;}

    public IfElseData(string id, int x, int y)
    {
        ConditionId = id;
        X = x;
        Y = y;
    }

    public void updateAll(bool inTrueBranch, string curId)
    {
        if (inTrueBranch)
        {
            LastTrueId = curId;
        }
        else
        {
            LastFalseId = curId;
        }
    }

}

public enum ConnectType { IF, ELSE, FOR, NONE }

public class DrawIoGenerator
{
    public static string _name = "";

    public const int xOffset = 200;
    public const int yOffset = 100;

    public static int xStart = 200;
    public static int yStart = 0;

    static int min_x = xStart;
    static int max_x = 0;
    static int max_y = 0;

    static List<string> _types = new List<string>() {"void", "int", "double", "float", "char", "string"};

    private static XElement ProcessShape(string s, int x, int y, string id)
    {
        if (x < min_x)
        {
            min_x = x;
        }
        if (x > max_x)
        {
            max_x = x;
        }
        if (y > max_y)
        {
            max_y = y;
        }

        if ((_types.Any(s.Contains) && s.Contains("{") && s.Contains("(") && s.Contains(")")) || s.Contains("return") && !s.Contains("for"))
        {
            return RoundBox(id, s.Replace('{', ' ').TrimEnd(), x, y);
        }
        else if (s.Contains("for"))
        {
            return ForLoop(id, s.Replace('{', ' ').TrimEnd(), x, y);
        }
        else if (s.Contains("cin") || s.Contains("cout"))
        {
            return CinCout(id, s.TrimStart(), x, y);
        }
        else if (s.Contains("if") && !s.Contains("else"))
        {
            return IfStatement(id, s.Replace('{', ' ').Trim(), x, y);
        }
        else if (s.Contains("while"))
        {
            return WhileLoop(id, s.Replace('{', ' ').TrimEnd(), x, y);
        }
        else if (s.Trim().Length > 5 && !s.Contains("else"))
        {
            return Box(id, s.Trim(), x, y);
        }
        return null;
    }
    public static List<XElement> ProcessFunc(List<string> funcAndBody, int startX, int startY)
    {
        string lastTrueElem = "";
        xStart = startX;
        yStart = startY;
        string uuid = GenerateRandomString(19);
        List<XElement> elements = new List<XElement>();
        Stack<ConnectData> stack = new Stack<ConnectData>();
        Stack<IfElseData> ifStack = new Stack<IfElseData>();
        int curX = xStart, curY = yStart + yOffset, c = 0, p = 0;
        List<string> contentList = funcAndBody;        
        for (int i = 0; i < contentList.Count; ++i)
        {        
            var s = contentList[i];
            
            var id = uuid + c;

            if (s.Contains("{") && i != 0)
            {
                if (s.Contains("else"))
                {
                    curX = ifStack.Peek().X;
                    curY = ifStack.Peek().Y;
                    elements.Add(Arrow(id + "_elsearrow", ifStack.Peek().ConditionId, uuid + (c + 1).ToString()));
                }
                stack.Push(new ConnectData(curX, curY, id, s));
                if (s.Contains("if"))
                {
                    ifStack.Push(new IfElseData(id, curX, curY));
                }
            }
            var o = ProcessShape(s, curX, curY, id);
            lastTrueElem = o == null ? lastTrueElem : id;
            elements.Add(o);
            if (s.Contains("if"))
            {
                curX -= xOffset;
                curY += yOffset;
            }
            if (s.Contains("}") && stack.Count > 0)
            {
                if (stack.Peek().line.Contains("else") && s.Contains("else"))
                {
                    var t = stack.Pop();
                    var b = stack.Pop();
                    stack.Push(t);
                    stack.Push(b);
                }
                var q = stack.Pop();
                var _else = q.line.Contains("else");
                var _if = q.line.Contains("if");
                var _for = q.line.Contains("for");
                var _while = q.line.Contains("while");
                if (ifStack.Count > 0)
                {
                    var _lastIf = ifStack.Peek();
                    if (_if)
                    {
                        _lastIf.LastTrueId = uuid + (c - 1).ToString();
                        _lastIf.TrueX = curX;
                        curX = q.x;

                        if (!contentList[i + 1].Contains("else") && !s.Contains("else"))
                        {
                            //типа обычный иф
                            c++;
                            elements.Add(CreatePointCell(uuid + c + "_mid", curX, max_y + yOffset));
                            max_y += yOffset;
                            curY += yOffset;
                            elements.Add(IfElseEndArrow(uuid + c + "_arrow2mid0", _lastIf.LastTrueId, uuid + c + "_mid", curX - xOffset, max_y - yOffset / 2, curX));
                            elements.Add(IfElseEndArrow(uuid + c + "_arrow2mid1", q.connection_start, uuid + c + "_mid", curX + xOffset, max_y - yOffset / 2, curX));
                            ifStack.Pop();
                            max_x = int.Max(curX + (int) (1.5*xOffset), max_x);         
                            min_x = int.Min(curX - (int) (1.5*xOffset), min_x);               
                        }
                        else
                        {
                            curY = q.y + yOffset;
                        }
                    }
                    else if (_else && !s.Contains("else"))
                    {

                        _lastIf.LastFalseId = uuid + (c - 1).ToString();
                        _lastIf.FalseX = curX;
                        curX = q.x;
                        curY = max_y;
                        c++;
                        // сведение 2 стрелок блять
                        elements.Add(CreatePointCell(uuid + c + "_mid", _lastIf.TrueX + (_lastIf.FalseX - _lastIf.TrueX) / 2, max_y + 2 * yOffset));
                        elements.Add(IfElseEndArrow(uuid + c + "_arrow2mid0", _lastIf.LastTrueId, uuid + c + "_mid", _lastIf.TrueX, max_y + yOffset / 2, _lastIf.TrueX + (_lastIf.FalseX - _lastIf.TrueX) / 2));
                        elements.Add(IfElseEndArrow(uuid + c + "_arrow2mid1", _lastIf.LastFalseId, uuid + c + "_mid", _lastIf.FalseX, max_y + yOffset / 2, _lastIf.TrueX + (_lastIf.FalseX - _lastIf.TrueX) / 2));
                        max_x = int.Max(curX + (int) (1.5*xOffset), max_x);  
                        min_x = int.Min(curX - (int) (1.5*xOffset), min_x);
                        ifStack.Pop();
                    }
                }
                if (_for || _while)
                {
                    // Last shape inside the loop (body)
                    string lastBodyId = uuid + (c - 1).ToString();
                    // Condition shape
                    string conditionId = q.connection_start;

                    // --- Loop‑back arrow: from last body to condition ---
                    int loopBackX = min_x;               // X of the last body shape (right side)
                    int loopBackY = q.y;                // Y of the condition
                    int midY = curY;       // Y halfway down from the last body

                    elements.Add(ForEndArrow(uuid + c + "_for_loop_arrow" + conditionId,
                        lastBodyId, conditionId,
                        loopBackX, loopBackY + 30, midY));
                    elements.Add(ForEndArrow(uuid + c + "_for_loop_arrow" + conditionId + "_mid",
                        lastBodyId + "_mid", conditionId,
                        loopBackX, loopBackY, midY));

                    // --- Exit arrow: from condition to the next shape ---
                    // Determine the ID of the shape that follows the loop\
                    if (stack.Count > 0 && (stack.Peek().line.Contains("for") || stack.Peek().line.Contains("while")) && contentList[i+1].Contains("}") && i + 1 < contentList.Count - 1 )
                    {
                        elements.Add(ForEndForArrow(uuid + c + "_for_exit_arrow"+ conditionId, conditionId, stack.Peek().connection_start, max_x + 10, q.y, max_y + 10, stack.Peek().x - xOffset - 10, stack.Peek().y + 30));
                        min_x -= 10;
                        curX = max_x + (int)(1.5*xOffset);
                        curY -= yOffset / 2;   
                    } else
                    {
                        string nextShapeId;
                        if (i + 1 < contentList.Count && (contentList[i + 1].Contains("}") || contentList[i + 1].Contains("if")))
                            nextShapeId = uuid + c.ToString();               // next shape not yet created
                        else
                            nextShapeId = uuid + (c + 1).ToString();         // shape after the closing brace



                        elements.Add(Arrow(uuid + c + "_for_exit_arrow"+ conditionId, conditionId, nextShapeId));
                        min_x -= 10;
                        // Adjust current X for subsequent statements
                        // Place the next shape to the right of the loop's widest point
                        curX = max_x + (int)(1.5*xOffset);
                        curY -= yOffset / 2;   
                    }
                }
                // все остальные циклы блеать

            }
            if (s.Contains("else"))
            {
                curX = max_x + xOffset;                
                curY += yOffset;
            }
            if (lastTrueElem == id)
            {
                curY += yOffset;
                c++;
                p = 0;
            }
            else
            {
                p++;
                if (p == 1)
                {
                    c++;
                }
            }
        }        
        int n = 0;
        for (int i = 1; i < c; ++i)
        {
            var r = new Random();
            elements.Add(Arrow(uuid + i + "_arrow" + n, uuid + (i - 1).ToString(), uuid + i));
            n++;
        }
        return elements;
    }

    public static void CreateSimpleDiagram()
    {
        Console.WriteLine("cpp file name (with extention): ");
        _name = Console.ReadLine();
        List<string> contentList = new List<string>();
        List<List<string>> funcs = new List<List<string>>();
        if (File.Exists(_name))
        {
            contentList = File.ReadAllLines(_name).ToList();
        }
        int c = 0;
        List<string> _cur = new List<string>();
        List<string> _types = new List<string>() {"void", "int", "double", "float", "char", "string"};
        int braceDepth = 0;
        bool inFunction = false;
        List<string> currentFunction = null;

        foreach (string line in contentList)
        {
            string trimmed = line.Trim();

            // Detect function start: line contains a return type, '(', ')', and not a control keyword
            if (!inFunction &&
                _types.Any(t => line.Contains(t)) &&
                line.Contains("(") && line.Contains(")") &&
                !line.Contains("for") && !line.Contains("while") &&
                !line.TrimStart().StartsWith("if"))
            {
                // Start a new function
                inFunction = true;
                currentFunction = [line];

                // Count braces already in this line
                braceDepth = line.Count(c => c == '{') - line.Count(c => c == '}');
                continue;
            }

            if (inFunction)
            {
                currentFunction.Add(line);

                // Update brace depth for this line
                braceDepth += line.Count(c => c == '{');
                braceDepth -= line.Count(c => c == '}');

                // If depth returns to zero, the function ended
                if (braceDepth == 0)
                {
                    funcs.Add(currentFunction);
                    inFunction = false;
                    currentFunction = null;
                }
            }
        }
        List<XElement> xElements = new List<XElement>();        

        funcs.ForEach(o =>
        {
           var t = ProcessFunc(o, xStart, yStart);
           xElements.AddRange(t);
           xStart = max_x + 3 * xOffset;
           max_x = xStart;
           min_x = xStart - xOffset/4;
           max_y = yStart;
        });

        //xElements.AddRange(ProcessFunc(funcs[1], 800, 0));
        var r = Root(xElements);
        

        new XDocument(new XDeclaration("1.0", "utf-8", "yes"), r).Save(_name + "_diagram.xml"); ;
    }

    static XElement Root(List<XElement> elements)
    {
        return new XElement("mxfile",
            new XAttribute("host", "app.diagrams.net"),
            new XAttribute("agent", "Mozilla/5.0 (X11; Linux x86_64; rv:140.0) Gecko/20100101 Firefox/140.0"),
            new XAttribute("version", "29.3.8"),
                new XElement("diagram", new XAttribute("name", "Page-1"), new XAttribute("id", "iUAUeTz2SxgbpLWaFMRQ"),
                new XElement("mxGraphModel",
                new XElement("root",
                new XElement("mxCell", new XAttribute("id", "0")),
                new XElement("mxCell", new XAttribute("id", "1"), new XAttribute("parent", "0")),
                elements))));
    }

    static XElement Arrow(string id, string source, string target)
    {
        return new XElement("mxCell",
            new XAttribute("id", id),
            new XAttribute("value", ""),
            new XAttribute("edge", "1"),
            new XAttribute("style", "edgeStyle=orthogonalEdgeStyle;rounded=0;orthogonalLoop=1;jettySize=auto;html=1;endArrow=classic;"),
            new XAttribute("parent", "1"),
            new XAttribute("source", source),
            new XAttribute("target", target),
            new XElement("mxGeometry",
                new XAttribute("relative", "1"),
                new XAttribute("as", "geometry")
            )
        );
    }

    static XElement ForEndArrow(string id, string source, string target, int x, int y, int low_y)
    {
        return new XElement("mxCell",
            new XAttribute("id", id),
            new XAttribute("value", ""),
            new XAttribute("edge", "1"),
            new XAttribute("style", "edgeStyle=orthogonalEdgeStyle;rounded=0;orthogonalLoop=1;jettySize=auto;html=1;entryX=0;entryY=0.5;entryDx=0;entryDy=0;"),
            new XAttribute("parent", "1"),
            new XAttribute("source", source),
            new XAttribute("target", target),
            new XElement("mxGeometry",
                new XAttribute("relative", "1"),
                new XAttribute("as", "geometry"),
                new XElement("Array",
                    new XAttribute("as", "points"),
                    new XElement("mxPoint", new XAttribute("x", x), new XAttribute("y", low_y)),
                    new XElement("mxPoint", new XAttribute("x", x - xOffset / 2), new XAttribute("y", low_y)),
                    new XElement("mxPoint", new XAttribute("x", x - xOffset / 2), new XAttribute("y", y))
                )
            )
        );
    }

    static XElement ForEndForArrow(string id, string source, string target, int x, int y, int low_y, int low_x, int high_y)
    {
        return new XElement("mxCell",
            new XAttribute("id", id),
            new XAttribute("value", ""),
            new XAttribute("edge", "1"),
            new XAttribute("style", "edgeStyle=orthogonalEdgeStyle;rounded=0;orthogonalLoop=1;jettySize=auto;html=1;entryX=0;entryY=0.5;entryDx=0;entryDy=0;"),
            new XAttribute("parent", "1"),
            new XAttribute("source", source),
            new XAttribute("target", target),
            new XElement("mxGeometry",
                new XAttribute("relative", "1"),
                new XAttribute("as", "geometry"),
                new XElement("Array",
                    new XAttribute("as", "points"),
                    new XElement("mxPoint", new XAttribute("x", x), new XAttribute("y", y)),
                    new XElement("mxPoint", new XAttribute("x", x), new XAttribute("y", low_y)),
                    new XElement("mxPoint", new XAttribute("x", low_x), new XAttribute("y", low_y)),
                    new XElement("mxPoint", new XAttribute("x", low_x), new XAttribute("y", high_y))
                )
            )
        );
    }

    static XElement WhileLoop(string id, string value, int x, int y, int height = 80, int width = 150)
    {
        return new XElement("mxCell",
            new XAttribute("id", id),
            new XAttribute("value", value),
            new XAttribute("style", "rhombus;whiteSpace=wrap;html=1;"),
            new XAttribute("parent", "1"),
            new XAttribute("vertex", "1"),
            new XElement("mxGeometry",
                new XAttribute("height", height),
                new XAttribute("width", width),
                new XAttribute("x", x - width / 2),
                new XAttribute("y", y),
                new XAttribute("as", "geometry")
            )
        );
    }



    static XElement RoundBox(string id, string value, int x, int y, int height = 60, int width = 120)
    {
        return new XElement("mxCell",
                        new XAttribute("id", id),
                        new XAttribute("value", value),
                        new XAttribute("vertex", "1"),
                        new XAttribute("style", "rounded=1;whiteSpace=wrap;html=1;"),
                        new XAttribute("parent", "1"),
                        new XElement("mxGeometry",
                            new XAttribute("height", height),
                            new XAttribute("width", width),
                            new XAttribute("x", x - width / 2),
                            new XAttribute("y", y),
                            new XAttribute("as", "geometry")
                        )
                    );
    }

    // Creates an invisible point cell
    static XElement CreatePointCell(string id, int x, int y)
    {
        return new XElement("mxCell",
            new XAttribute("id", id),
            new XAttribute("vertex", "1"),
            new XAttribute("style", "ellipse;html=1;fillColor=none;strokeColor=none;pointerEvents=0;"),
            new XAttribute("parent", "1"),
            new XElement("mxGeometry",
                new XAttribute("x", x),  // center at (x, y) with a 2x2 bounding box
                new XAttribute("y", y),
                new XAttribute("width", "2"),
                new XAttribute("height", "2"),
                new XAttribute("as", "geometry")
            )
        );
    }

    // Creates an arrow from sourceId to the point cell
    static XElement IfElseEndArrow(string arrowId, string sourceId, string pointId, int x, int y, int mid_x)
    {
        return new XElement("mxCell",
            new XAttribute("id", arrowId),
            new XAttribute("value", ""),
            new XAttribute("edge", "1"),
            new XAttribute("style", "edgeStyle=orthogonalEdgeStyle;rounded=0;html=1;endArrow=classic;"),
            new XAttribute("parent", "1"),
            new XAttribute("source", sourceId),
            new XAttribute("target", pointId),
            new XElement("mxGeometry",
                new XAttribute("relative", "1"),
                new XAttribute("as", "geometry"),
                                new XElement("Array",
                    new XAttribute("as", "points"),
                    new XElement("mxPoint", new XAttribute("x", x), new XAttribute("y", y)),
                    new XElement("mxPoint", new XAttribute("x", mid_x), new XAttribute("y", y))
                )
            )
        );
    }

    static XElement Box(string id, string value, int x, int y, int height = 60, int width = 120)
    {
        return new XElement("mxCell",
                        new XAttribute("id", id),
                        new XAttribute("value", value),
                        new XAttribute("vertex", "1"),
                        new XAttribute("style", "whiteSpace=wrap;html=1;"),
                        new XAttribute("parent", "1"),
                        new XElement("mxGeometry",
                            new XAttribute("height", height),
                            new XAttribute("width", width),
                            new XAttribute("x", x - width / 2),
                            new XAttribute("y", y),
                            new XAttribute("as", "geometry")
                        )
                    );
    }


    static XElement CinCout(string id, string value, int x, int y, int height = 60, int width = 120)
    {
        return new XElement("mxCell",
                        new XAttribute("id", id),
                        new XAttribute("value", value),
                        new XAttribute("vertex", "1"),
                        new XAttribute("style", "shape=parallelogram;perimeter=parallelogramPerimeter;whiteSpace=wrap;html=1;fixedSize=1;"),
                        new XAttribute("parent", "1"),
                        new XElement("mxGeometry",
                            new XAttribute("height", height),
                            new XAttribute("width", width),
                            new XAttribute("x", x - width / 2),
                            new XAttribute("y", y),
                            new XAttribute("as", "geometry")
                        )
                    );
    }

    static XElement ForLoop(string id, string value, int x, int y, int height = 40, int width = 290)
    {
        return new XElement("mxCell",
                        new XAttribute("id", id),
                        new XAttribute("parent", "1"),
                        new XAttribute("style", "shape=hexagon;perimeter=hexagonPerimeter2;whiteSpace=wrap;html=1;fixedSize=1;"),
                        new XAttribute("value", value),
                        new XAttribute("vertex", "1"),
                        new XElement("mxGeometry",
                            new XAttribute("height", height),
                            new XAttribute("width", width),
                            new XAttribute("x", x - width / 2),
                            new XAttribute("y", y + height / 2),
                            new XAttribute("as", "geometry")
                        )
                    );
    }

    static XElement SwitchCase(string id, string value, int x, int y)
    {
        return new XElement("mxCell",
                        new XAttribute("id", id),
                        new XAttribute("parent", "1"),
                        new XAttribute("style", "shape=hexagon;perimeter=hexagonPerimeter2;whiteSpace=wrap;html=1;fixedSize=1;"),
                        new XAttribute("value", value),
                        new XAttribute("vertex", "1"),
                        new XElement("mxGeometry",
                            new XAttribute("height", "40"),
                            new XAttribute("width", "290"),
                            new XAttribute("x", x),
                            new XAttribute("y", y),
                            new XAttribute("as", "geometry")
                        )
                    );
    }

    static XElement IfStatement(string id, string value, int x, int y, int height = 80, int width = 160)
    {
        return new XElement("mxCell",
                        new XAttribute("id", id),
                        new XAttribute("parent", "1"),
                        new XAttribute("style", "rhombus;whiteSpace=wrap;html=1;"),
                        new XAttribute("value", value),
                        new XAttribute("vertex", "1"),
                        new XElement("mxGeometry",
                            new XAttribute("height", height),
                            new XAttribute("width", width),
                            new XAttribute("x", x - width / 2),
                            new XAttribute("y", y + height / 2),
                            new XAttribute("as", "geometry")
                        )
                    );
    }


    static string GenerateRandomString(int length)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        var result = new StringBuilder(length);
        for (int i = 0; i < length; i++)
        {
            result.Append(chars[random.Next(chars.Length)]);
        }
        return result.ToString();
    }
}