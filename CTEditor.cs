using UnityEngine;
using UnityEditor;
using CTEdtorSp;
using System.Collections.Generic;

namespace CTEdtorSp{
    public enum MouseKey
    {
        MOUSE_LEFT      =           0,
        MOUSE_RIGHT     =           1,
        MOUSE_CENTER    =           2,
        MAX             =           3
    }
    public enum CTEVarType
    {
        VAR_INT         =           0,
        VAR_UINT        =           1,
        VAR_LONG        =           2,
        VAR_ULONG       =           3,
        VAR_STRING      =           5,
        VAR_FLOAT       =           6,
        VAR_DOUBLE      =           7,
    }
    //基本色块
	class DisplayItemBase{ 
		public Rect     _rect       = new Rect ();
		public Color[]  _colors     = new Color[] { Color.green, Color.white, Color.red, Color.blue, Color.red};
		public string   name;
		public Vector2  Pos { set { _rect.position = value; } get { return _rect.position; } }
		public Color    _curColor   = Color.green;
		public DisplayItemBase()
		{
		}
		public DisplayItemBase(Vector2 pos, float w, float h)
		{
			Pos = pos;
			_rect.width = w;
			_rect.height = h;
		}
	}
    //具有拖动的色块
	class OperatorItem : DisplayItemBase{
		public  bool isDownSel = false;
        public bool isRightSel = false;
        public OperatorItem _childItem;//与其相关联的孩子节点.
        public OperatorItem _fatherItem;//父节点.
        public Vector2 half;
        public Vector2 cOffset;
		public OperatorItem()
		{
            half = new Vector2(_rect.width/2, _rect.height/2);
		}
		public OperatorItem(Vector2 pos, float w, float h) : base(pos, w, h)
		{
            half = new Vector2(_rect.width / 2, _rect.height / 2);
		}
		public bool InRange(Vector2 pos)
		{
			return (pos.x >= _rect.x && pos.x <= _rect.x + _rect.width &&  pos.y >= _rect.y && pos.y < _rect.y + _rect.height); 
		}
        public void OnDraw()
        {

            //绘制与子节点的链接
            if (null != _childItem)
            {
                CTEditor.DrawLine(Pos + half, _childItem.Pos + _childItem.half);
            }
            //绘制当节点自身样子
            DrawSelf();
        }
        private void DrawSelf()
        {
            EditorGUI.DrawRect(_rect, _curColor);
        }
        //添加一个子节点
        public void SetChild(OperatorItem item)
        {
            if(null == item)
            {
                return;
            }
            if(null != _childItem)
            {
                _childItem._fatherItem = null; //清除父节点
            }
            _childItem = item;
            if(null != item._fatherItem)
            {
                item._fatherItem._childItem = null; 
            }
            item._fatherItem = this;
        }
	}
    class VarData
    {
        public System.Type type;
        public object obj;
        public VarData(object ob, System.Type ty)
        {
            obj = ob;
            type = ty;
        }
    }
    class VariableManager : Singleton<VariableManager>
    {
        public Dictionary<string,VarData> varMap = new Dictionary<string, VarData>(); //
        /*
        public Values
        {
            get {return va;}
        }
        */
        public VarData GetVar(string name)
        {
            VarData obj = null;
            if(varMap.ContainsKey(name))
            {
                varMap.TryGetValue(name, out obj);
            }
            return obj;
        }
        public void AddVal(string name, VarData obj)
        {
            if(!varMap.ContainsKey(name))
            {
                varMap.Add(name, obj);
            }
        }
    }
}
public class CTEditor : EditorWindow {
	static  List<OperatorItem> ois             =         new List<OperatorItem>();
    static  Rect               dr              =         new Rect();
    private Vector2            scrollPos;
    private Rect               drawPanelRect;
    int                        toolBarIdx;
    string[]                   toolBarContent   =        new string [] {"Task","Variable","Inspector"};

    bool                       buttonListOpen   =        false;
    string[]                   buttonList       =        new string[] {"Sequence","Selector","Random Selector","Paralel Sequence", "Parallel"};
    bool                       buttonActionOpen =        false;
    string[]                   buttonActionList =        new string[] {"Wait", "Random", "Move To", "Idle", "Attack"};
    CTEVarType varType = CTEVarType.VAR_STRING;
    string varName = "";
    int valInt = 0;
    string varString = "";
    float floatVar = 0.0f;
    double doubleVar = 0;

	[MenuItem("Window/CTEditor")]
	static void Init(){
		CTEditor ew = (CTEditor)GetWindow (typeof(CTEditor));
		ew.Show ();
        ois.Clear();
		for (int i = 0; i < 3; i++) {
			OperatorItem oi = new OperatorItem (new Vector2 (10, i * 40), 100, 60);
			ois.Add (oi);
		}
	}
    //绘制一条线
	public static void DrawLine(Vector2  begin, Vector2 end, float bounds = 3)
	{
        Vector2 nor = begin;
        float dx = end.x - begin.x;
        float dy = end.y - begin.y;
        dr.width = dr.height = bounds;

        int mx = Mathf.CeilToInt(Mathf.Abs(dx) / bounds);
        int my = Mathf.CeilToInt(Mathf.Abs(dy) / bounds);
        if (Mathf.Abs(dx) > Mathf.Abs(dy))
        {
            for (int i = 0; i < mx; i++)
            {
                dr.x = nor.x;
                dr.y = nor.y;

                EditorGUI.DrawRect(dr, Color.yellow);
                nor.x += bounds * (dx > 0 ? 1 : -1);
                nor.y += dy / mx;
            }
        }
        else
        {
            for (int i = 0; i < my; i++)
            {
                dr.x = nor.x;
                dr.y = nor.y;

                EditorGUI.DrawRect(dr, Color.yellow);
                nor.y += bounds * (dy > 0 ? 1 : -1);
                nor.x += dx / my;
            }
        }
	}
    //获取绘制面板的坐标（流程绘制面板）
    private Vector2 GetPos(Vector2 pos)
    {
        return pos + scrollPos - drawPanelRect.position;
    }
	void OnGUI()
	{
        EditorGUILayout.BeginHorizontal();                                                                  //整体布局
        EditorGUILayout.BeginVertical(GUILayout.Width(300));                                                //左侧布局
        GUILayout.Space(3);
        toolBarIdx = GUILayout.Toolbar(toolBarIdx, toolBarContent);                                         //视口选择框
        GUILayout.Space(3);
        if (toolBarIdx == 0)
        {
            buttonListOpen = EditorGUILayout.Foldout(buttonListOpen,"Structor");
            if (buttonListOpen)
            {
                for (int i = 0; i < buttonList.Length; i++)
                {
                    GUILayout.Button(buttonList[i]);
                }
            }
            buttonActionOpen = EditorGUILayout.Foldout(buttonActionOpen, "Action");
            if (buttonActionOpen)
            {
                for (int i = 0; i < buttonActionList.Length; i++)
                {
                    GUILayout.Button(buttonActionList[i]);
                }
            }
        }
        else if (toolBarIdx == 1)
        {

            varType = (CTEVarType)EditorGUILayout.EnumPopup("ValType", varType);
            varName = EditorGUILayout.TextField("VarName", varName);
            System.Type valType = typeof(int);
            object valObj = (object)0;
            switch(varType)
            {
                case CTEVarType.VAR_INT:
                    valType = typeof(int);
                    valInt = EditorGUILayout.IntField("Content", valInt);
                    valObj = valInt;
                    break;
                case CTEVarType.VAR_UINT:
                    valType = typeof(uint);
                    valInt = EditorGUILayout.IntField("Content", valInt);
                    valObj = valInt;
                    break;
                case CTEVarType.VAR_ULONG:
                    valType = typeof(ulong);
                    valInt = EditorGUILayout.IntField("Content", valInt);
                    valObj = valInt;
                    break;
                case CTEVarType.VAR_LONG:
                    valType = typeof(long);
                    valInt = EditorGUILayout.IntField("Content", valInt);
                    valObj = valInt;
                    break;
                case CTEVarType.VAR_STRING:
                    valType = typeof(string);
                    varString = EditorGUILayout.TextField("Content", varString);
                    valObj = varString;
                    break;
                case CTEVarType.VAR_FLOAT:
                    valType = typeof(float);
                    floatVar = EditorGUILayout.FloatField("Content", floatVar);
                    valObj = floatVar;
                    break;
                case CTEVarType.VAR_DOUBLE:
                    valType = typeof(double);
                    doubleVar = EditorGUILayout.DoubleField("Content", doubleVar);
                    valObj = doubleVar;
                    break;
            }
            
            if(GUILayout.Button("AddVal"))
            {
               if(!string.IsNullOrEmpty(varName))
               {
                   VarData vd = new VarData(valObj, valType);
                   VariableManager.Instance.AddVal(varName, vd);
               }
            }
            
            Dictionary<string, VarData>.Enumerator e = VariableManager.Instance.varMap.GetEnumerator();
            while(e.MoveNext())
            {
                EditorGUILayout.TextField(e.Current.Key, e.Current.Value.obj.ToString());
            }
        }
        else
        {

        }

        EditorGUILayout.EndVertical();
        drawPanelRect = EditorGUILayout.BeginVertical();                                                    //右侧绘制窗口
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);                                             //自适应视口
        GUILayout.Label("", GUILayout.Width(5000), GUILayout.Height(5000));
        EditorGUI.DrawRect(new Rect(0, 0, 5001, 5001), Color.black);
        //draw Item
        for (int i = 0; i < ois.Count; i++)
        {
            ois[i].OnDraw();
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
        OnInput();                                                                                          //接受输入并且处理
		Repaint ();                                                                                         //重新绘制
	}
    private void OnInput()
    {
        Vector2 mousePosOnPanel = GetPos(Event.current.mousePosition);
        if (Event.current.type == EventType.MouseDrag)
        { 
            for (int i = 0; i < ois.Count; i++)
            {
                if (ois[i].isDownSel)
                {
                    ois[i].Pos = mousePosOnPanel - ois[i].cOffset;
                }
            }
        }

        if (Event.current.type == EventType.mouseDown)
        {
            MouseKey mouseKey = (MouseKey)Event.current.button;
            for (int i = 0; i < ois.Count; i++)
            {
                if (ois[i].InRange(mousePosOnPanel))
                {
                    if (MouseKey.MOUSE_LEFT == mouseKey)
                    {
                        ois[i]._curColor = ois[i]._colors[1];
                        ois[i].isDownSel = true;
                        ois[i].cOffset = mousePosOnPanel - ois[i].Pos;
                    }
                    if (MouseKey.MOUSE_RIGHT == mouseKey)
                    {
                        int j = 0;
                        for (j = 0; j < ois.Count; j++)
                        {
                            if (ois[j].isRightSel)
                            {
                                break;
                            }
                        }
                        if (j < ois.Count) //找到
                        {
                            if (ois[i] != ois[j])
                            {
                                ois[i]._childItem = ois[j];
                                //ois[i].isRightSel = false;
                            }
                        }
                        else
                        {
                            ois[i]._curColor = ois[i]._colors[2];
                            ois[i].isRightSel = true;
                        }
                    }
                    break;
                }
            }
        }
        if (Event.current.type == EventType.mouseUp)
        {
            if (MouseKey.MOUSE_LEFT == (MouseKey)Event.current.button)
            {
                for (int i = 0; i < ois.Count; i++)
                {
                    ois[i].isDownSel = false;
                    ois[i]._curColor = ois[i]._colors[0];
                }
            }
            else if (MouseKey.MOUSE_RIGHT == (MouseKey)Event.current.button)
            {
                int j = 0;
                for (j = 0; j < ois.Count; j++)
                {
                    if (ois[j].isRightSel)
                    {
                        break;
                    }
                }
                if (j < ois.Count) //找到
                {
                    for (int i = 0; i < ois.Count; i++)
                    {
                        ois[i].isRightSel = false;
                        ois[i]._curColor = ois[i]._colors[0];
                        if (ois[i].InRange(mousePosOnPanel) && ois[i] != ois[j])
                        {
                            ois[j].SetChild(ois[i]);
                        }
                    }
                }
            }
        }
    }
}
