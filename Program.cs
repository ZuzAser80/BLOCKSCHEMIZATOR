using System.Collections.Generic;
using System.Xml.Linq;
using System.Text;
using System.Net;
using System.Runtime.CompilerServices;
using System.ComponentModel;
using System.Data.Common;
using System.Reflection.Metadata;
using System.ComponentModel.Design.Serialization;
using System.Runtime.Serialization;
using System.Runtime.InteropServices;
using Microsoft.VisualBasic;

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
    public string MidId { get; set; }
    public int X { get; set; }
    public int Y { get; set; }

    public int TrueX { get; set; }
    public int FalseX { get; set; }

    public int TrueY { get; set; }
    public int FalseY { get; set; }

    public IfElseData(string id, int x, int y)
    {
        ConditionId = id;
        X = x;
        Y = y;
    }

    public void updateAll(bool inTrueBranch, string curId, int curY)
    {
        if (inTrueBranch)
        {
            LastTrueId = curId;
            TrueY = curY;
        }
        else
        {
            LastFalseId = curId;
            FalseY = curY;
        }
    }

}

public class DrawIoGenerator
{
    const int widthMultiplier = 6;
    public static string _name = "";

    public const int xOffset = 200;
    public const int yOffset = 100;

    public static int xStart = 0;
    public static int yStart = 0;

    static int min_x = xStart;
    static int max_x = 0;
    static int max_y = 0;

    static List<string> _types = new List<string>() { "void", "int", "double", "float", "char", "string", "struct", "class", "string*", "int*", "char*", "double*", "float*" };
    static List<string> _funcs = new List<string>();
    static string _bracket = "";

    private static XElement ProcessShape(string s, int x, int y, string id)
    {
        if (s.Trim().StartsWith("//") || s.Trim().StartsWith("/*") || s.Trim().EndsWith("\\*"))
        {
            return null;
        }
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


        if ((_types.Any(s.Contains) && s.Contains("{") && !s.Contains("for")) || (s.Contains("return") && s.Trim().EndsWith(";")))
        {
            return RoundBox(id, s.Replace('{', ' ').TrimEnd(), x, y);
        }
        else if (_funcs.Any(s.Contains))
        {
           return FuncCallBox(id, s, x, y);
        } 
        else if (s.Trim().StartsWith("for"))
        {
            return ForLoop(id, s.Replace('{', ' ').TrimEnd(), x, y);
        }
        else if (s.Trim().StartsWith("getline") || s.Trim().StartsWith("printf") || s.Trim().StartsWith("cin") || s.Trim().StartsWith("cout"))
        {
            return CinCout(id, s.TrimStart(), x, y);
        }
        else if ((s.Trim().StartsWith("if") && !s.Contains("else") && !s.Contains("ifstream")) || s.Trim().Contains("switch"))
        {
            return IfStatement(id, s.Replace('{', ' ').Trim(), x, y);
        }
        else if (s.Trim().StartsWith("while"))
        {
            return WhileLoop(id, s.Replace('{', ' ').TrimEnd(), x, y);
        }
        else if (s.Trim().Length > 2 && !s.Contains("else"))
        {
            return Box(id, s.Replace('{', ' ').Trim(), x, y);
        }         
        return null;
    }
    
    public static List<XElement> ProcessFunc(List<string> funcAndBody, int startX, int startY, out int funcWidth)
    {
        string lastTrueElem = "";
        xStart = startX;
        yStart = startY;
        bool offsetDown = true;
        string uuid = GenerateRandomString(19);
        List<XElement> elements = new List<XElement>();
        Stack<ConnectData> stack = new Stack<ConnectData>();
        Stack<IfElseData> ifStack = new Stack<IfElseData>();
        Stack<int> loopLocalMinX = new Stack<int>();
        Stack<string> loopLocalIfMidId = new Stack<string>();
        Stack<int> loopNestingDepth = new Stack<int>();
        string lastCaseId = "";
        int stackMaxX = 0;
        int curX = xStart, curY = yStart + yOffset, c = 0, p = 0;
        List<string> contentList = funcAndBody;
        List<int> noConnectSourceIndex = new List<int>();
        for (int i = 0; i < contentList.Count; ++i)
        {
            var s = contentList[i];

            var id = uuid + c;

            if ((s.Contains("{") && !s.Contains("{}") && i != 0) || s.Trim().StartsWith("case") || s.Trim().StartsWith("default"))
            {
                if ((s.Trim().StartsWith("case") || s.Trim().StartsWith("default")) && lastCaseId.Count() > 0)
                {
                    elements.Add(Arrow(uuid + c + "_case_arrow2", lastCaseId, id.ToString()));
                }
                if (s.Trim().StartsWith("else") && !s.Trim().StartsWith("if"))
                {
                    curX = ifStack.Peek().X;
                    curY = ifStack.Peek().Y;
                    elements.Add(Arrow(id + "_elsearrow", ifStack.Peek().ConditionId, uuid + (c + 1).ToString()));
                }
                stack.Push(new ConnectData(curX, curY, id, s));
                if (s.Trim().StartsWith("if"))
                {
                    ifStack.Push(new IfElseData(id, curX, curY));
                }
                if (s.Trim().StartsWith("for") || s.Trim().StartsWith("while"))
                {
                    loopLocalMinX.Push(curX);
                    loopNestingDepth.Push(loopNestingDepth.Count);
                }
            }
            var o = ProcessShape(s, curX, curY, id);
            lastTrueElem = o == null ? lastTrueElem : id;
            elements.Add(o);
            if (loopLocalMinX.Count > 0 && o != null)
            {
                if (curX < loopLocalMinX.Peek())
                {
                    loopLocalMinX.Pop();
                    loopLocalMinX.Push(curX);
                }
            }
            ConnectData q = null;
            if (s.Trim().StartsWith("if") && !s.Contains("ifstream"))
            {
                elements.Add(Text(uuid + c + "1_text", "1", curX - xOffset / 2, curY, 30, 60, "fontSize=21;"));
                elements.Add(Text(uuid + c + "0_text", "0", curX + xOffset / 2, curY, 30, 60, "fontSize=21;"));
                curX -= xOffset;                
            }
            if (((s.Contains("}") && !s.Contains("{}")) || (s.Contains("break;") && stack.Peek().line.Trim().StartsWith("case"))) && stack.Count > 0)
            {
                if (s.Contains("else if"))
                {
                    max_x -= xOffset;
                    c++;
                    elements.Add(IfStatement(uuid + c, s.Replace('}', ' ').Replace('{', ' ').Trim(), max_x + xOffset, curY + yOffset));                                        
                    continue;
                }
                if (stack.Peek().line == s)
                {
                    var t = stack.Pop();
                    var b = stack.Pop();
                    stack.Push(t);
                    stack.Push(b);
                }
                q = stack.Peek();
                var _else = q.line.Contains("else") && !q.line.Contains("else if");
                var _if = q.line.Trim().StartsWith("if");
                var _for = q.line.Trim().StartsWith("for");
                var _while = q.line.Trim().StartsWith("while");
                var _switch = q.line.Trim().StartsWith("switch");
                var _case = q.line.Trim().StartsWith("case") || q.line.Trim().StartsWith("default");
                var _struct = q.line.Trim().StartsWith("struct") || q.line.Trim().StartsWith("class");
                if (ifStack.Count > 0)
                {
                    var _lastIf = ifStack.Peek();
                    if (_if)
                    {
                        _lastIf.LastTrueId = uuid + (c - 1).ToString();
                        _lastIf.TrueX = curX;
                        _lastIf.TrueY = curY;
                        curX = q.x;

                        if (i < contentList.Count - 1 && !contentList[i + 1].Contains("else") && !s.Contains("else"))
                        {
                            int midX = curX;
                            int trueY = _lastIf.TrueY;
                            int falseY = _lastIf.TrueY;
                            int midY = trueY + (falseY - trueY) / 2 + 60;
                            
                            elements.Add(CreatePointCell(uuid + c + "_mid", midX, midY));
                            curY = midY + yOffset;
                            if (loopLocalIfMidId.Count > 0)
                            {
                                loopLocalIfMidId.Pop();
                            }
                            loopLocalIfMidId.Push(uuid + c + "_mid");
                            elements.Add(IfElseEndArrow(uuid + c + "_arrow2mid0", _lastIf.LastTrueId, uuid + c + "_mid", curX - xOffset, trueY, midX));
                            elements.Add(IfElseEndArrow(uuid + c + "_arrow2mid1", q.connection_start, uuid + c + "_mid", curX + xOffset, falseY, midX));
                            ifStack.Pop();
                            max_x = int.Max(curX + (int)(1.5 * xOffset), max_x);
                            min_x = int.Min(curX - (int)(1.5 * xOffset), min_x);
                            curY -= yOffset;
                        }
                        else
                        {
                            curY = q.y;                            
                        }                        
                    }
                    if (_else)
                    {
                        _lastIf.LastFalseId = uuid + (c - 1).ToString();
                        _lastIf.FalseX = curX;
                        _lastIf.FalseY = curY;
                        curX = q.x;
                        curY = max_y;
                        int midX = _lastIf.TrueX + (_lastIf.FalseX - _lastIf.TrueX) / 2;
                        int midY = _lastIf.TrueY + (_lastIf.FalseY - _lastIf.TrueY) / 2 + 60;
                        elements.Add(CreatePointCell(uuid + c + "_mid", midX, midY));
                        if (loopLocalIfMidId.Count > 0)
                        {
                            loopLocalIfMidId.Pop();
                        }
                        loopLocalIfMidId.Push(uuid + c + "_mid");
                        elements.Add(IfElseEndArrow(uuid + c + "_arrow2mid0", _lastIf.LastTrueId, uuid + c + "_mid", _lastIf.TrueX, _lastIf.TrueY, midX));
                        elements.Add(IfElseEndArrow(uuid + c + "_arrow2mid1", _lastIf.LastFalseId, uuid + c + "_mid", _lastIf.FalseX, _lastIf.FalseY, midX));
                        max_x = int.Max(curX + (int)(1.5 * xOffset), max_x);
                        min_x = int.Min(curX - (int)(1.5 * xOffset), min_x);
                        ifStack.Pop();
                        curX = midX;
                        curY = midY;
                    }
                }
                if (_for || _while)
                {
                    noConnectSourceIndex.Add(c);
                    string lastBodyId = uuid + (c - 1).ToString();
                    string conditionId = q.connection_start;

                    int loopBackX = (loopLocalMinX.Count > 0 ? loopLocalMinX.Peek() : min_x) - xOffset;
                    int loopBackY = q.y;
                    int midY = curY;

                    elements.Add(Text(uuid + c + "for1_text", "1", q.x + 40, q.y + yOffset/2, 30, 60, "fontSize=21;"));
                    // верим в то что у нас есть выход
                    elements.Add(Text(uuid + c + "for0_text", "0", q.x + xOffset, q.y - 10, 30, 60, "fontSize=21;"));
                    if (c-1 - int.Parse(string.Concat(q.connection_start.ToArray().Reverse().TakeWhile(char.IsNumber).Reverse())) > 1)
                    {
                        // типа > 1 строки в for
                        elements.Add(Text(uuid + c + "for_brace_text", "{", q.x - 40, q.y + yOffset/2, 30, 60, "fontSize=21;"));
                        elements.Add(Text(uuid + c + "for_brace_end_text", "}", curX + 40, curY - yOffset/2, 30, 60, "fontSize=21;"));
                    }

                    string loopSourceId = lastBodyId;
                    string loopSourceIdMid = lastBodyId + "_mid";
                    bool useIfMidSource = loopLocalIfMidId.Count > 0;
                    if (useIfMidSource)
                    {
                        loopSourceId = loopLocalIfMidId.Peek();
                        loopSourceIdMid = loopSourceId;
                    }

                    if (useIfMidSource)
                    {
                        elements.Add(ForEndArrow(uuid + c + "_for_loop_arrow" + conditionId,
                            loopSourceId, conditionId,
                            loopBackX, loopBackY + 30, midY));
                    }
                    else
                    {
                        elements.Add(ForEndArrow(uuid + c + "_for_loop_arrow" + conditionId,
                            loopSourceId, conditionId,
                            loopBackX, loopBackY + 30, midY));
                        elements.Add(ForEndArrow(uuid + c + "_for_loop_arrow" + conditionId + "_mid",
                            loopSourceIdMid, conditionId,
                            loopBackX, loopBackY, midY));
                    }


                    bool hasOuterLoop = stack.Count > 1 && (stack.ElementAt(1).line.Trim().StartsWith("for") || stack.ElementAt(1).line.Trim().StartsWith("while"));
                    
                    if (hasOuterLoop)
                    {
                        var outerLoop = stack.ElementAt(1);
                        int depth = loopNestingDepth.Count;
                        elements.Add(ForEndForArrow(uuid + c + "_for_exit_arrow" + conditionId, conditionId, outerLoop.connection_start, max_x + 10 + (depth * xOffset), q.y + (depth * yOffset), max_y + 10 + (depth * yOffset), outerLoop.x - xOffset - 10 - (depth * xOffset), outerLoop.y + 30));
                        min_x -= 10;
                        curX = max_x + (int)(1.5 * xOffset);
                        curY -= yOffset / 2;
                    }
                    else
                    {
                        string nextShapeId = uuid + c.ToString();
                        elements.Add(Arrow(uuid + c + "_for_exit_arrow" + conditionId, conditionId, nextShapeId));
                        min_x -= 10;
                        curX = max_x + (int)(1.5 * xOffset);
                        curY -= yOffset / 2;
                    }
                }
                // все остальные циклы блеать
                // TODO: Switch-case
                if (_case)
                {
                    noConnectSourceIndex.Add(c + 1);
                    //noConnectSourceIndex.Add(c);
                    

                    //Console.WriteLine("2141413414 : " + contentList[i + 2].Trim() + " : " + s);
                    curX = q.x;
                    curY += yOffset;      
                    offsetDown = true;         
                    lastCaseId = q.connection_start;     
                }
                if (_switch)
                {
                    elements.Add(Arrow(uuid + c.ToString() + "_switch", lastCaseId, uuid + c.ToString()));                    
                }
                if (q != null && (q.line.Trim().StartsWith("for") || q.line.Trim().StartsWith("while")) && loopLocalMinX.Count > 0)
                {
                    loopLocalMinX.Pop();
                    if (loopLocalIfMidId.Count > 0)
                    {
                        loopLocalIfMidId.Pop();
                    }
                    if (loopNestingDepth.Count > 0)
                    {
                        loopNestingDepth.Pop();
                    }
                }
                stack.Pop();
            }
            if (s.Trim().Contains("else") && q != null)
            {
                noConnectSourceIndex.Add(c);
                elements.Add(Arrow(uuid + c + "_else_connect", q.connection_start, uuid + c));
                curX = max_x + xOffset;
                curY += yOffset;
            }
            if (s.Trim().StartsWith("case"))
            {
                if (stack.TryPeek(out ConnectData res) && res.line.Trim().StartsWith("case")) {
                    elements.Add(Arrow(uuid + c + "_case_arrow", stack.Peek().connection_start, uuid + (c + 1).ToString()));
                }
                if (contentList[i - 1].Trim().StartsWith("}"))
                {
                    elements.Add(Arrow(uuid + c + "_case_arrow1", stack.Peek().connection_start, uuid + (c + 2).ToString()));
                }
                offsetDown = false;
            }
            if (o != null)
            {
                if (offsetDown) {
                    curY += yOffset;
                } else
                {
                    curX += xOffset;
                }
                c++;                
            }
        }
        int n = 0;
        for (int i = 1; i < c; ++i)
        {
            var r = new Random();
            if (!noConnectSourceIndex.Contains(i)) {
                elements.Add(Arrow(uuid + i + "_arrow" + n, uuid + (i - 1).ToString(), uuid + i));
                n++;
            }
        }
        funcWidth = max_x - xStart;
        return elements;
    }

    public static void CreateSimpleDiagram()
    {
        Console.WriteLine("cpp file name (with extention): ");
        _name = Console.ReadLine();
        if (_name.StartsWith('"') && _name.EndsWith('"'))
        {
            _name = _name.Replace('"', ' ');
        }
        List<string> contentList = new List<string>();
        List<List<string>> funcs = new List<List<string>>();
        if (File.Exists(_name))
        {
            contentList = File.ReadAllLines(_name).ToList();
        }
        int c = 0;
        List<string> _cur = new List<string>();
        int braceDepth = 0;
        bool inFunction = false;
        bool inStruct = false;
        bool justExitedFunction = false;
        List<string> _funcNames = new List<string>();
        List<string> currentFunction = new List<string>();
        List<string> currentStruct = new List<string>();

        foreach (string line in contentList)
        {
            string trimmed = line.Trim();

            if(trimmed.Contains("struct") || trimmed.Contains("class"))
            {
                inStruct = true;
            }

            if (trimmed.Contains("#include") || trimmed.Contains("using"))
            {
                _bracket += trimmed + "\n";
            }

            if (!inFunction &&
                _types.Any(t => line.Contains(t)) &&
                line.Contains("(") && line.Contains(")") &&
                !line.Contains("for") && !line.Contains("while") &&
                !line.TrimStart().Contains("if"))
            {
                _funcs.Add(line.Split('(')[0].Replace(_types.Find(line.Contains), "").Trim());
                if (!inStruct) {
                    inFunction = true;
                    currentFunction = [line];
                }
                if (inStruct) {
                    _funcNames.Add(line.Split('(')[0].Replace(_types.Find(line.Contains), "").Trim());
                }

                //braceDepth = line.Count(c => c == '{') - line.Count(c => c == '}');
            }

            if (inStruct && line.Contains("{") && !line.Trim().StartsWith("struct") && line.Trim().StartsWith("class") &&
            line.Contains("(") && line.Contains(")") &&
                !line.Contains("for") && !line.Contains("while") &&
                !line.TrimStart().Contains("if"))
            {
                braceDepth = 1;
                inFunction = true;
                currentFunction = [line];
                continue;
            }

            if (inStruct && !inFunction)
            {
                if (justExitedFunction && line.Trim().StartsWith("}"))
                {
                    justExitedFunction = false;
                    continue;
                }
                justExitedFunction = false;
                currentStruct.Add(line);
                if (line.Trim() == "};")
                {
                    currentStruct.AddRange(_funcNames);
                    Console.WriteLine("ADDING: " + currentStruct[0]);
                    funcs.Add([.. currentStruct]);
                    inStruct = false;                    
                    currentStruct.Clear();
                    _funcNames.Clear();    
                }
            }

            if (inFunction)
            {
                currentFunction.Add(line);

                braceDepth += line.Count(c => c == '{');
                braceDepth -= line.Count(c => c == '}');

                if (braceDepth == 0)
                {
                    Console.WriteLine("ADDIN1111: " + currentFunction[0]);
                    funcs.Add(currentFunction);
                    inFunction = false;
                    currentFunction = null;
                    if (inStruct)
                    {
                        justExitedFunction = true;
                    }
                }
            }
        }
        List<XElement> xElements = new List<XElement>();
        int prevFuncWidth = 0;

        funcs.ForEach(o =>
        {
            if (o.Count > 0 && o[0].Contains("main"))
            {
                xElements.Add(Text("maintext", _bracket, xStart + 2 *xOffset, yStart + yOffset, yOffset, xOffset * 2, "align=left;fontSize=21;"));
            }
            var t = ProcessFunc(o, xStart, yStart, out int funcWidth);
            xElements.AddRange(t);
            xStart = xStart + funcWidth + 2 * xOffset;
            max_x = xStart;
            min_x = xStart - xOffset / 4;
            max_y = yStart;
        });

        var r = Root(xElements);

        var path = Path.Combine(Path.GetDirectoryName(_name), Path.GetFileNameWithoutExtension(_name));
        Console.WriteLine(path);
        if (path.Contains("/ /"))
        {
            path = path.Split("/ /")[1];
        }
        Console.WriteLine(path);
        new XDocument(new XDeclaration("1.0", "utf-8", "yes"), r).Save(path +"_diagram.xml"); ;
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

    static XElement Text(string id, string value, int x, int y, int height, int width, string style)
    {
        string encodedValue = System.Net.WebUtility.HtmlEncode(value);
        return new XElement("mxCell",
            new XAttribute("id", id),
            new XAttribute("value", value),
            new XAttribute("style", "text;whiteSpace=wrap;strokeColor=none;fillColor=none;align=center;verticalAlign=middle;rounded=0;" + style),
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

    static XElement ForEndArrow(string id, string source, string target, int x, int y, int low_y)
    {
        return new XElement("mxCell",
            new XAttribute("id", id),
            new XAttribute("value", ""),
            new XAttribute("edge", "1"),
            new XAttribute("style", "edgeStyle=orthogonalEdgeStyle;rounded=0;orthogonalLoop=1;jettySize=auto;html=1;endArrow=classic;exitX=1;exitY=0.5;exitDx=0;exitDy=0;entryX=0;entryY=0.5;entryDx=0;entryDy=0;"),
            new XAttribute("parent", "1"),
            new XAttribute("source", source),
            new XAttribute("target", target),
            new XElement("mxGeometry",
                new XAttribute("relative", "1"),
                new XAttribute("as", "geometry"),
                new XElement("Array",
                    new XAttribute("as", "points"),
                    new XElement("mxPoint", new XAttribute("x", x), new XAttribute("y", low_y)),
                    new XElement("mxPoint", new XAttribute("x", x), new XAttribute("y", y))
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
            new XAttribute("style", "edgeStyle=orthogonalEdgeStyle;rounded=0;orthogonalLoop=1;jettySize=auto;html=1;exitX=1;exitY=0.5;exitDx=0;exitDy=0;entryX=0;entryY=0.5;entryDx=0;entryDy=0;"),
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
        width = value.Length * widthMultiplier + 20;
        return new XElement("mxCell",
                        new XAttribute("id", id),
                        new XAttribute("value", value),
                        new XAttribute("vertex", "1"),
                        new XAttribute("style", "ellipse;whiteSpace=wrap;html=1;"),
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
        width = value.Length * widthMultiplier + 20;
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

    static XElement FuncCallBox(string id, string value, int x, int y, int height = 60, int width = 120)
    {
        width = value.Length * widthMultiplier + 20;
        return new XElement("mxCell",
                        new XAttribute("id", id),
                        new XAttribute("value", value),
                        new XAttribute("vertex", "1"),
                        new XAttribute("style", "shape=process;whiteSpace=wrap;html=1;backgroundOutline=1"),
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
        width = value.Length * widthMultiplier + 20;
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
                            new XAttribute("y", y),
                            new XAttribute("as", "geometry")
                        )
                    );
    }

    static string GenerateRandomString(int length)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyz";
        var random = new Random();
        var result = new StringBuilder(length);
        for (int i = 0; i < length; i++)
        {
            result.Append(chars[random.Next(chars.Length)]);
        }
        return result.ToString();
    }
}