using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace KPA.Editor
{
    public class SpecToSceneBuilder : EditorWindow
    {
        private string specText = "";
        private Vector2 scrollPos;

        [MenuItem("KPA/Spec To Scene Builder")]
        public static void ShowWindow()
        {
            GetWindow<SpecToSceneBuilder>("Spec To Scene Builder");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Hierarchy Specification (Paste here):", EditorStyles.boldLabel);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            specText = EditorGUILayout.TextArea(specText, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("Build Hierarchy", GUILayout.Height(40)))
            {
                BuildHierarchy();
            }
        }

        private void BuildHierarchy()
        {
            if (string.IsNullOrEmpty(specText)) return;

            string[] lines = specText.Split('\n');
            Dictionary<int, GameObject> depthToParent = new Dictionary<int, GameObject>();
            
            GameObject rootParent = Selection.activeGameObject;
            depthToParent[-1] = rootParent;

            Undo.IncrementCurrentGroup();
            int groupId = Undo.GetCurrentGroup();

            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                int depth = GetDepth(line);
                string cleanLine = CleanPrefix(line);
                
                // 이름 추출 및 유효성 검사
                string namePart = ExtractName(cleanLine);
                if (string.IsNullOrEmpty(namePart)) continue; // 이름이 없으면 생성 안함
                
                string typePart = ExtractType(cleanLine);
                
                GameObject newObj = CreateGameObject(namePart, typePart, depthToParent.ContainsKey(depth - 1) ? depthToParent[depth - 1] : rootParent);
                if (newObj == null) continue;

                Undo.RegisterCreatedObjectUndo(newObj, "Build Spec Object");
                ApplyProperties(newObj, cleanLine);

                depthToParent[depth] = newObj;
            }

            Undo.CollapseUndoOperations(groupId);
            Debug.Log("[SpecToSceneBuilder] Hierarchy built successfully!");
        }

        private int GetDepth(string line)
        {
            // 줄 시작부터 첫 영문/숫자/브래킷이 나타나는 위치를 깊이로 계산
            int index = 0;
            for (int i = 0; i < line.Length; i++)
            {
                if (System.Char.IsLetterOrDigit(line[i]) || line[i] == '[')
                {
                    index = i;
                    break;
                }
            }
            // 약 4개 공백(또는 기호)당 1단계 깊이로 계산
            return index / 4;
        }

        private string CleanPrefix(string line)
        {
            return Regex.Replace(line, @"^[│\s├└─]+", "").Trim();
        }

        private string ExtractName(string line)
        {
            int parenIndex = line.IndexOf('(');
            if (parenIndex == -1) return line.Trim();
            return line.Substring(0, parenIndex).Trim();
        }

        private string ExtractType(string line)
        {
            Match match = Regex.Match(line, @"\(([^/]+)");
            if (match.Success) return match.Groups[1].Value.Trim();
            return "";
        }

        private GameObject CreateGameObject(string name, string type, GameObject parent)
        {
            GameObject obj;
            
            if (type.Contains("TMP_Text"))
            {
                obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            }
            else if (type.Contains("UI Button"))
            {
                obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                // 버튼은 자식으로 텍스트를 하나 가짐이 일반적
                GameObject txt = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                txt.transform.SetParent(obj.transform, false);
                var tmp = txt.GetComponent<TextMeshProUGUI>();
                tmp.text = name;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.black;
                SetFullStretch(txt.GetComponent<RectTransform>());
            }
            else if (type.Contains("UI Image") || type.Contains("UI Panel"))
            {
                obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                if (type.Contains("UI Panel")) 
                {
                    obj.GetComponent<Image>().color = new Color(1, 1, 1, 0.4f);
                }
            }
            else if (type.Contains("Empty RectTransform"))
            {
                obj = new GameObject(name, typeof(RectTransform));
            }
            else if (type.Contains("Canvas"))
            {
                obj = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                
                // Canvas 설정 (Overlay)
                Canvas canvas = obj.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                // CanvasScaler 설정 (1920x1080)
                CanvasScaler scaler = obj.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
            }
            else if (type.Contains("UI ScrollRect"))
            {
                obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect));
                ScrollRect sr = obj.GetComponent<ScrollRect>();
                sr.horizontal = false;
                sr.vertical = true;
                // 배경은 보통 투명하게 하거나 반투명
                obj.GetComponent<Image>().color = new Color(1, 1, 1, 0.1f);
                
                // Viewport 생성
                GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
                viewport.transform.SetParent(obj.transform, false);
                viewport.GetComponent<Mask>().showMaskGraphic = false;
                SetFullStretch(viewport.GetComponent<RectTransform>());
                sr.viewport = viewport.GetComponent<RectTransform>();
            }
            else if (type.Contains("LineRenderer"))
            {
                obj = new GameObject(name, typeof(LineRenderer));
                LineRenderer lr = obj.GetComponent<LineRenderer>();
                lr.startWidth = 0.1f;
                lr.endWidth = 0.1f;
                lr.useWorldSpace = true;
                lr.positionCount = 0;
                lr.material = new Material(Shader.Find("Sprites/Default"));
                lr.startColor = Color.yellow;
                lr.endColor = Color.yellow;
            }
            else
            {
                obj = new GameObject(name);
            }

            if (parent != null)
            {
                obj.transform.SetParent(parent.transform, false);
                
                // ScrollRect의 자식인 경우 Content 자동 연결 시도 (이름 기반)
                if (parent.name.Contains("Scroll") && name.Contains("Content"))
                {
                    ScrollRect sr = parent.GetComponent<ScrollRect>();
                    if (sr != null) sr.content = obj.GetComponent<RectTransform>();
                }
            }

            // [추가] 기본적으로 사이즈를 0,0으로 초기화하여 Stretch 시 오프셋 방지
            RectTransform rt = obj.GetComponent<RectTransform>();
            if (rt != null) rt.sizeDelta = Vector2.zero;
            
            return obj;
        }

        private void ApplyProperties(GameObject obj, string line)
        {
            RectTransform rect = obj.GetComponent<RectTransform>();
            if (rect == null) return;

            // 1. 활성화 상태 (Active)
            string activeStr = MatchValue(line, "Active");
            if (!string.IsNullOrEmpty(activeStr)) 
                obj.SetActive(activeStr.ToLower() == "true");

            // 2. 태그 처리 (Image OFF, Raycast Target)
            Image img = obj.GetComponent<Image>();
            if (img != null)
            {
                if (line.Contains("[Image OFF]")) img.enabled = false;
                if (line.Contains("[Raycast Target OFF]")) img.raycastTarget = false;
                if (line.Contains("[Raycast Target ON]")) img.raycastTarget = true;
            }

            // 3. RectTransform 속성
            string anchorStr = MatchValue(line, "Anchor");
            if (!string.IsNullOrEmpty(anchorStr)) SetAnchor(rect, anchorStr);

            string pivotStr = MatchValue(line, "Pivot");
            if (!string.IsNullOrEmpty(pivotStr)) rect.pivot = ParseVector2(pivotStr);

            string sizeStr = MatchValue(line, "Size");
            if (!string.IsNullOrEmpty(sizeStr)) rect.sizeDelta = ParseVector2(sizeStr);

            string posStr = MatchValue(line, "Pos");
            if (!string.IsNullOrEmpty(posStr)) rect.anchoredPosition = ParseVector2(posStr);
            
            string posX = MatchValue(line, "PosX");
            if (float.TryParse(posX, out float fx)) rect.anchoredPosition = new Vector2(fx, rect.anchoredPosition.y);
            
            string posY = MatchValue(line, "PosY");
            if (float.TryParse(posY, out float fy)) rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, fy);

            string left = MatchValue(line, "Left");
            if (float.TryParse(left, out float fL)) rect.offsetMin = new Vector2(fL, rect.offsetMin.y);
            string right = MatchValue(line, "Right");
            if (float.TryParse(right, out float fR)) rect.offsetMax = new Vector2(-fR, rect.offsetMax.y);
            string bottom = MatchValue(line, "Bottom");
            if (float.TryParse(bottom, out float fB)) rect.offsetMin = new Vector2(rect.offsetMin.x, fB);
            string top = MatchValue(line, "Top");
            if (float.TryParse(top, out float fT)) rect.offsetMax = new Vector2(rect.offsetMax.x, -fT);

            // 3. 비주얼 속성 (Color, Alpha)
            if (img != null)
            {
                string colorStr = MatchValue(line, "Color");
                if (!string.IsNullOrEmpty(colorStr)) img.color = ParseColor(colorStr);

                string alphaStr = MatchValue(line, "Alpha");
                if (float.TryParse(alphaStr, out float fAlpha)) 
                {
                    Color c = img.color;
                    c.a = fAlpha;
                    img.color = c;
                }
            }

            // 4. 文本 속성 (FontSize, Alignment)
            TextMeshProUGUI tmp = obj.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                string fontSizeStr = MatchValue(line, "FontSize");
                if (float.TryParse(fontSizeStr, out float fSize)) tmp.fontSize = fSize;

                string alignStr = MatchValue(line, "Alignment");
                if (!string.IsNullOrEmpty(alignStr))
                {
                    if (alignStr.ToLower().Contains("left")) tmp.alignment = TextAlignmentOptions.Left;
                    else if (alignStr.ToLower().Contains("right")) tmp.alignment = TextAlignmentOptions.Right;
                    else tmp.alignment = TextAlignmentOptions.Center;
                }
            }
        }

        private Color ParseColor(string str)
        {
            if (str.StartsWith("#"))
            {
                if (ColorUtility.TryParseHtmlString(str, out Color color)) return color;
            }
            else if (str.ToLower() == "black") return Color.black;
            else if (str.ToLower() == "white") return Color.white;
            else if (str.ToLower() == "red") return Color.red;
            else if (str.ToLower() == "blue") return Color.blue;
            else if (str.ToLower() == "green") return Color.green;
            else if (str.ToLower() == "yellow") return Color.yellow;
            
            // R,G,B 형태 지원
            string[] parts = str.Split(',');
            if (parts.Length >= 3)
            {
                if (float.TryParse(parts[0], out float r) && float.TryParse(parts[1], out float g) && float.TryParse(parts[2], out float b))
                {
                    float a = parts.Length > 3 && float.TryParse(parts[3], out float vA) ? vA / 255f : 1f;
                    return new Color(r / 255f, g / 255f, b / 255f, a);
                }
            }

            return Color.white;
        }

        private string MatchValue(string text, string key)
        {
            // 다음 속성 구분자(/)나 다음 속성의 시작(Key:) 전까지를 값으로 인식
            // 정규표현식: key + ": " 이후부터 ( / 또는 다음 대문자로 시작하는 단어: ) 전까지
            Match match = Regex.Match(text, key + @":\s*([^/\])]+?)(?=\s*/|\s+[A-Z][a-zA-Z]+:|$)");
            if (match.Success) return match.Groups[1].Value.Trim();
            
            // 위 복합 패턴 실패 시 단순 패턴 시도 (슬래시나 괄호에서 중단)
            match = Regex.Match(text, key + @":\s*([^/\])]+)");
            if (match.Success) return match.Groups[1].Value.Trim();

            return "";
        }

        private Vector2 ParseVector2(string str)
        {
            // 숫자, 점, 마이너스, 콤마, x, 공백 보존
            string clean = Regex.Replace(str, @"[^\d\.\-,x\s]", ""); 
            string[] parts = clean.Split(new char[] { ',', 'x', ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            
            if (parts.Length >= 2)
            {
                if (float.TryParse(parts[0], out float x) && float.TryParse(parts[1], out float y))
                {
                    return new Vector2(x, y);
                }
            }
            else if (parts.Length == 1)
            {
                if (float.TryParse(parts[0], out float val))
                {
                    return new Vector2(val, val);
                }
            }
            return Vector2.zero;
        }

        private void SetAnchor(RectTransform rect, string anchor)
        {
            anchor = anchor.ToLower();
            if (anchor.Contains("stretch-stretch")) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; }
            else if (anchor.Contains("top-center")) { rect.anchorMin = new Vector2(0.5f, 1); rect.anchorMax = new Vector2(0.5f, 1); }
            else if (anchor.Contains("bottom-center")) { rect.anchorMin = new Vector2(0.5f, 0); rect.anchorMax = new Vector2(0.5f, 0); }
            else if (anchor.Contains("middle-left")) { rect.anchorMin = new Vector2(0, 0.5f); rect.anchorMax = new Vector2(0, 0.5f); }
            else if (anchor.Contains("middle-right")) { rect.anchorMin = new Vector2(1, 0.5f); rect.anchorMax = new Vector2(1, 0.5f); }
            else if (anchor.Contains("middle-center")) { rect.anchorMin = new Vector2(0.5f, 0.5f); rect.anchorMax = new Vector2(0.5f, 0.5f); }
            else if (anchor.Contains("top-left")) { rect.anchorMin = new Vector2(0, 1); rect.anchorMax = new Vector2(0, 1); }
            else if (anchor.Contains("top-right")) { rect.anchorMin = Vector2.one; rect.anchorMax = Vector2.one; }
            else if (anchor.Contains("bottom-left")) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.zero; }
            else if (anchor.Contains("bottom-right")) { rect.anchorMin = new Vector2(1, 0); rect.anchorMax = new Vector2(1, 0); }
        }

        private void SetFullStretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
