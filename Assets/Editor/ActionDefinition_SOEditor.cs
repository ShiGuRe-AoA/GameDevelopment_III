// Assets/Editor/ActionDefinition_SOEditor.cs
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

[CustomEditor(typeof(ActionDefinition_SO))]
public class ActionDefinition_SOEditor : Editor
{
    private SerializedProperty animatorStateNameProp;
    private SerializedProperty clipProp;
    private SerializedProperty eventsProp;
    private SerializedProperty rangesProp;
    private SerializedProperty lockMoveProp;
    private SerializedProperty lockInteractProp;

    private const float TimelineHeight = 140f;
    private const float HeaderHeight = 22f;
    private const float TrackHeight = 28f;

    private const float TopMarkerY = 34f;
    private const float CenterLineY = 70f;
    private const float BottomMarkerY = 106f;

    private const float LeftPadding = 14f;
    private const float RightPadding = 14f;

    private const float MarkerWidth = 12f;
    private const float MarkerHeight = 12f;

    private float previewTime = 0f;
    private bool isDraggingMarker = false;

    private enum DragTargetType
    {
        None,
        Event,
        RangeStart,
        RangeEnd
    }

    private DragTargetType dragType = DragTargetType.None;
    private int dragIndex = -1;

    private void OnEnable()
    {
        animatorStateNameProp = serializedObject.FindProperty("AnimatorStateName");
        clipProp = serializedObject.FindProperty("Clip");
        eventsProp = serializedObject.FindProperty("Events");
        rangesProp = serializedObject.FindProperty("Ranges");
        lockMoveProp = serializedObject.FindProperty("LockMove");
        lockInteractProp = serializedObject.FindProperty("LockInteract");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawBasicFields();

        EditorGUILayout.Space(8);

        DrawPreviewPanel();

        EditorGUILayout.Space(8);

        Rect timelineRect = GUILayoutUtility.GetRect(
            EditorGUIUtility.currentViewWidth - 40f,
            TimelineHeight,
            GUILayout.ExpandWidth(true)
        );

        DrawTimeline(timelineRect);
        EditorGUILayout.Space(2);
        if (GUILayout.Button("重置"))
        {
            Reset();
        }
        EditorGUILayout.Space(8);

        DrawDefaultInspectorLists();


        serializedObject.ApplyModifiedProperties();
    }

    private void DrawBasicFields()
    {
        EditorGUILayout.PropertyField(animatorStateNameProp);

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(clipProp);
        if (EditorGUI.EndChangeCheck())
        {
            previewTime = 0f;
        }

        EditorGUILayout.PropertyField(lockMoveProp);
        EditorGUILayout.PropertyField(lockInteractProp);

        AnimationClip clip = clipProp.objectReferenceValue as AnimationClip;
        float duration = clip != null ? clip.length : 0f;

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.FloatField("Duration", duration);
        }
    }

    private void DrawDefaultInspectorLists()
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Timing Data", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(eventsProp, true);
        EditorGUILayout.PropertyField(rangesProp, true);
    }

    private void DrawTimeline(Rect rect)
    {
        ActionDefinition_SO def = (ActionDefinition_SO)target;
        AnimationClip clip = clipProp.objectReferenceValue as AnimationClip;

        EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f, 1f));
        GUI.Box(rect, GUIContent.none);

        if (clip == null || clip.length <= 0f)
        {
            DrawNoClipMessage(rect);
            return;
        }

        float duration = Mathf.Max(0.0001f, clip.length);

        Rect innerRect = new Rect(
            rect.x + LeftPadding,
            rect.y + 8f,
            rect.width - LeftPadding - RightPadding,
            rect.height - 16f
        );

        DrawTimelineBackground(innerRect);
        DrawTickLines(innerRect, duration);
        DrawRangeBars(innerRect, duration);
        DrawEventLines(innerRect, duration);
        HandleInput(innerRect, duration);
        DrawEventMarkers(innerRect, duration);
        DrawRangeMarkers(innerRect, duration);
        DrawTimeLabels(innerRect, duration);

        Repaint();
    }

    private void DrawNoClipMessage(Rect rect)
    {
        GUIStyle centered = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
        {
            fontSize = 12,
            alignment = TextAnchor.MiddleCenter
        };

        EditorGUI.LabelField(rect, "请先拖入 AnimationClip，时间轴会按 Clip.length 自动显示", centered);
    }

    private void DrawTimelineBackground(Rect rect)
    {
        Rect headerRect = new Rect(rect.x, rect.y, rect.width, HeaderHeight);
        Rect centerTrackRect = new Rect(rect.x, rect.y + CenterLineY - rect.y - TrackHeight * 0.5f, rect.width, TrackHeight);

        EditorGUI.DrawRect(headerRect, new Color(0.20f, 0.20f, 0.20f, 1f));
        EditorGUI.DrawRect(centerTrackRect, new Color(0.22f, 0.22f, 0.22f, 1f));

        Handles.color = new Color(0.45f, 0.45f, 0.45f, 1f);
        float y = rect.y + (CenterLineY - rect.y);
        Handles.DrawLine(new Vector2(rect.x, y), new Vector2(rect.xMax, y));
    }

    private void DrawTickLines(Rect rect, float duration)
    {
        int tickCount = GetTickCount(duration);

        for (int i = 0; i <= tickCount; i++)
        {
            float t = duration * i / tickCount;
            float x = TimeToX(t, rect, duration);

            Color lineColor = i == 0 || i == tickCount
                ? new Color(0.8f, 0.8f, 0.8f, 0.5f)
                : new Color(1f, 1f, 1f, 0.12f);

            Handles.color = lineColor;
            Handles.DrawLine(
                new Vector2(x, rect.y + HeaderHeight),
                new Vector2(x, rect.yMax)
            );

            GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.UpperCenter,
                normal = { textColor = Color.white }
            };

            Rect labelRect = new Rect(x - 25f, rect.y + 2f, 50f, 16f);
            EditorGUI.LabelField(labelRect, $"{t:0.00}", labelStyle);
        }
    }

    private void DrawEventLines(Rect rect, float duration)
    {
        for (int i = 0; i < eventsProp.arraySize; i++)
        {
            SerializedProperty eventProp = eventsProp.GetArrayElementAtIndex(i);
            SerializedProperty timeProp = eventProp.FindPropertyRelative("time");
            SerializedProperty nameProp = eventProp.FindPropertyRelative("name");

            float time = Mathf.Clamp(timeProp.floatValue, 0f, duration);
            float x = TimeToX(time, rect, duration);

            Handles.color = new Color(1f, 0.85f, 0.2f, 0.95f);
            Handles.DrawLine(
                new Vector2(x, rect.y + TopMarkerY + MarkerHeight * 0.5f),
                new Vector2(x, rect.y + CenterLineY)
            );

            Rect labelRect = new Rect(x - 45f, rect.y + 18f, 90f, 16f);
            GUIStyle style = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.UpperCenter,
                normal = { textColor = new Color(1f, 0.95f, 0.6f) }
            };
            EditorGUI.LabelField(labelRect, nameProp.stringValue, style);
        }
    }

    private void DrawRangeBars(Rect rect, float duration)
    {
        for (int i = 0; i < rangesProp.arraySize; i++)
        {
            SerializedProperty rangeProp = rangesProp.GetArrayElementAtIndex(i);
            SerializedProperty startProp = rangeProp.FindPropertyRelative("start");
            SerializedProperty endProp = rangeProp.FindPropertyRelative("end");
            SerializedProperty nameProp = rangeProp.FindPropertyRelative("name");

            float start = Mathf.Clamp(startProp.floatValue, 0f, duration);
            float end = Mathf.Clamp(endProp.floatValue, 0f, duration);

            if (end < start)
            {
                float temp = start;
                start = end;
                end = temp;
            }

            float xMin = TimeToX(start, rect, duration);
            float xMax = TimeToX(end, rect, duration);

            Rect barRect = new Rect(
                xMin,
                rect.y + CenterLineY + 8f,
                Mathf.Max(2f, xMax - xMin),
                14f
            );

            Color barColor = GetRangeColor(i);
            EditorGUI.DrawRect(barRect, barColor);

            Rect outlineRect = new Rect(barRect.x, barRect.y, barRect.width, barRect.height);
            Handles.color = new Color(0f, 0f, 0f, 0.6f);
            Handles.DrawAAPolyLine(
                1.5f,
                new Vector3(outlineRect.xMin, outlineRect.yMin),
                new Vector3(outlineRect.xMax, outlineRect.yMin),
                new Vector3(outlineRect.xMax, outlineRect.yMax),
                new Vector3(outlineRect.xMin, outlineRect.yMax),
                new Vector3(outlineRect.xMin, outlineRect.yMin)
            );

            GUIStyle style = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            EditorGUI.LabelField(barRect, nameProp.stringValue, style);
        }
    }

    private void DrawEventMarkers(Rect rect, float duration)
    {
        for (int i = 0; i < eventsProp.arraySize; i++)
        {
            SerializedProperty eventProp = eventsProp.GetArrayElementAtIndex(i);
            SerializedProperty timeProp = eventProp.FindPropertyRelative("time");

            float time = Mathf.Clamp(timeProp.floatValue, 0f, duration);
            float x = TimeToX(time, rect, duration);

            Rect markerRect = new Rect(
                x - MarkerWidth * 0.5f,
                rect.y + TopMarkerY,
                MarkerWidth,
                MarkerHeight
            );

            DrawTriangleMarker(markerRect, true, new Color(1f, 0.82f, 0.2f, 1f));
        }
    }

    private void DrawRangeMarkers(Rect rect, float duration)
    {
        for (int i = 0; i < rangesProp.arraySize; i++)
        {
            SerializedProperty rangeProp = rangesProp.GetArrayElementAtIndex(i);
            SerializedProperty startProp = rangeProp.FindPropertyRelative("start");
            SerializedProperty endProp = rangeProp.FindPropertyRelative("end");

            float start = Mathf.Clamp(startProp.floatValue, 0f, duration);
            float end = Mathf.Clamp(endProp.floatValue, 0f, duration);

            float xStart = TimeToX(start, rect, duration);
            float xEnd = TimeToX(end, rect, duration);

            Rect startRect = new Rect(
                xStart - MarkerWidth * 0.5f,
                rect.y + BottomMarkerY,
                MarkerWidth,
                MarkerHeight
            );

            Rect endRect = new Rect(
                xEnd - MarkerWidth * 0.5f,
                rect.y + BottomMarkerY,
                MarkerWidth,
                MarkerHeight
            );

            Color c = GetRangeColor(i);
            DrawTriangleMarker(startRect, false, c);
            DrawTriangleMarker(endRect, false, c);

            GUIStyle timeStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.UpperCenter,
                normal = { textColor = c }
            };

            Rect startTimeRect = new Rect(xStart - 28f, rect.y + BottomMarkerY + 13f, 56f, 16f);
            Rect endTimeRect = new Rect(xEnd - 28f, rect.y + BottomMarkerY + 27f, 56f, 16f);

            EditorGUI.LabelField(startTimeRect, $"{start:0.00}", timeStyle);
            EditorGUI.LabelField(endTimeRect, $"{end:0.00}", timeStyle);
        }
    }

    private void DrawTimeLabels(Rect rect, float duration)
    {
        GUIStyle leftStyle = new GUIStyle(EditorStyles.miniBoldLabel)
        {
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = Color.white }
        };

        GUIStyle rightStyle = new GUIStyle(EditorStyles.miniBoldLabel)
        {
            alignment = TextAnchor.MiddleRight,
            normal = { textColor = Color.white }
        };

        EditorGUI.LabelField(new Rect(rect.x, rect.yMax - 18f, 60f, 16f), "0.00s", leftStyle);
        EditorGUI.LabelField(new Rect(rect.xMax - 60f, rect.yMax - 18f, 60f, 16f), $"{duration:0.00}s", rightStyle);
    }

    private void HandleInput(Rect rect, float duration)
    {
        Event e = Event.current;
        int controlId = GUIUtility.GetControlID(FocusType.Passive);

        switch (e.type)
        {
            case EventType.MouseDown:
                if (e.button != 0) break;
                if (!rect.Contains(e.mousePosition)) break;

                if (TryPickEventMarker(e.mousePosition, rect, duration, out int eventIndex))
                {
                    dragType = DragTargetType.Event;
                    dragIndex = eventIndex;
                    isDraggingMarker = true;

                    SerializedProperty eventProp = eventsProp.GetArrayElementAtIndex(dragIndex);
                    previewTime = eventProp.FindPropertyRelative("time").floatValue;

                    GUIUtility.hotControl = controlId;
                    e.Use();
                    return;
                }

                if (TryPickRangeMarker(e.mousePosition, rect, duration, out int rangeIndex, out bool isStart))
                {
                    dragType = isStart ? DragTargetType.RangeStart : DragTargetType.RangeEnd;
                    dragIndex = rangeIndex;
                    isDraggingMarker = true;

                    SerializedProperty rangeProp = rangesProp.GetArrayElementAtIndex(dragIndex);
                    previewTime = isStart
                        ? rangeProp.FindPropertyRelative("start").floatValue
                        : rangeProp.FindPropertyRelative("end").floatValue;

                    GUIUtility.hotControl = controlId;
                    e.Use();
                    return;
                }
                break;

            case EventType.MouseDrag:
                if (GUIUtility.hotControl != controlId) break;
                if (dragType == DragTargetType.None || dragIndex < 0) break;

                Undo.RecordObject(target, "Drag Timeline Marker");

                float newTime = XToTime(e.mousePosition.x, rect, duration);

                if (dragType == DragTargetType.Event)
                {
                    SerializedProperty eventProp = eventsProp.GetArrayElementAtIndex(dragIndex);
                    SerializedProperty timeProp = eventProp.FindPropertyRelative("time");
                    timeProp.floatValue = Mathf.Clamp(newTime, 0f, duration);
                    previewTime = timeProp.floatValue;
                }
                else
                {
                    SerializedProperty rangeProp = rangesProp.GetArrayElementAtIndex(dragIndex);
                    SerializedProperty startProp = rangeProp.FindPropertyRelative("start");
                    SerializedProperty endProp = rangeProp.FindPropertyRelative("end");

                    if (dragType == DragTargetType.RangeStart)
                    {
                        startProp.floatValue = Mathf.Clamp(newTime, 0f, endProp.floatValue);
                        previewTime = startProp.floatValue;
                    }
                    else if (dragType == DragTargetType.RangeEnd)
                    {
                        endProp.floatValue = Mathf.Clamp(newTime, startProp.floatValue, duration);
                        previewTime = endProp.floatValue;
                    }
                }

                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
                Repaint();
                e.Use();
                break;

            case EventType.MouseUp:
                if (GUIUtility.hotControl != controlId) break;

                dragType = DragTargetType.None;
                dragIndex = -1;
                isDraggingMarker = false;
                GUIUtility.hotControl = 0;
                e.Use();
                break;
        }
    }

    private bool TryPickEventMarker(Vector2 mousePos, Rect rect, float duration, out int pickedIndex)
    {
        for (int i = eventsProp.arraySize - 1; i >= 0; i--)
        {
            SerializedProperty eventProp = eventsProp.GetArrayElementAtIndex(i);
            float time = eventProp.FindPropertyRelative("time").floatValue;
            float x = TimeToX(time, rect, duration);

            Rect pickRect = new Rect(
                x - 10f,
                rect.y + TopMarkerY - 6f,
                20f,
                24f
            );

            if (pickRect.Contains(mousePos))
            {
                pickedIndex = i;
                return true;
            }
        }

        pickedIndex = -1;
        return false;
    }

    private bool TryPickRangeMarker(Vector2 mousePos, Rect rect, float duration, out int pickedIndex, out bool isStart)
    {
        for (int i = rangesProp.arraySize - 1; i >= 0; i--)
        {
            SerializedProperty rangeProp = rangesProp.GetArrayElementAtIndex(i);

            float start = rangeProp.FindPropertyRelative("start").floatValue;
            float end = rangeProp.FindPropertyRelative("end").floatValue;

            float xStart = TimeToX(start, rect, duration);
            float xEnd = TimeToX(end, rect, duration);

            Rect startRect = new Rect(xStart - 10f, rect.y + BottomMarkerY - 4f, 20f, 24f);
            Rect endRect = new Rect(xEnd - 10f, rect.y + BottomMarkerY - 4f, 20f, 24f);

            if (startRect.Contains(mousePos))
            {
                pickedIndex = i;
                isStart = true;
                return true;
            }

            if (endRect.Contains(mousePos))
            {
                pickedIndex = i;
                isStart = false;
                return true;
            }
        }

        pickedIndex = -1;
        isStart = false;
        return false;
    }

    private void DrawTriangleMarker(Rect rect, bool pointDown, Color color)
    {
        Vector3 p1, p2, p3;

        if (pointDown)
        {
            p1 = new Vector3(rect.xMin, rect.yMin);
            p2 = new Vector3(rect.xMax, rect.yMin);
            p3 = new Vector3(rect.center.x, rect.yMax);
        }
        else
        {
            p1 = new Vector3(rect.xMin, rect.yMax);
            p2 = new Vector3(rect.xMax, rect.yMax);
            p3 = new Vector3(rect.center.x, rect.yMin);
        }

        // 填充
        Handles.color = color;
        Handles.DrawAAConvexPolygon(p1, p2, p3);

        // 描边
        Handles.color = new Color(0f, 0f, 0f, 0.8f);
        Handles.DrawAAPolyLine(2f, new Vector3[]
        {
        p1, p2, p3, p1
        });
    }

    private float TimeToX(float time, Rect rect, float duration)
    {
        float normalized = Mathf.Clamp01(time / duration);
        return Mathf.Lerp(rect.x, rect.xMax, normalized);
    }

    private float XToTime(float x, Rect rect, float duration)
    {
        float normalized = Mathf.InverseLerp(rect.x, rect.xMax, x);
        return Mathf.Clamp01(normalized) * duration;
    }

    private int GetTickCount(float duration)
    {
        if (duration <= 0.5f) return 5;
        if (duration <= 1.5f) return 6;
        if (duration <= 3f) return 8;
        if (duration <= 6f) return 10;
        return 12;
    }

    private Color GetRangeColor(int index)
    {
        Color[] colors =
        {
            new Color(0.30f, 0.75f, 1.00f, 0.85f),
            new Color(1.00f, 0.45f, 0.35f, 0.85f),
            new Color(0.45f, 0.90f, 0.45f, 0.85f),
            new Color(0.95f, 0.75f, 0.25f, 0.85f),
            new Color(0.75f, 0.45f, 1.00f, 0.85f),
        };

        return colors[index % colors.Length];
    }

    private void DrawPreviewPanel()
    {
        AnimationClip clip = clipProp.objectReferenceValue as AnimationClip;
        float duration = clip != null ? Mathf.Max(0.0001f, clip.length) : 0f;

        EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

        Rect outerRect = GUILayoutUtility.GetRect(160f, 180f, GUILayout.ExpandWidth(true));
        GUI.Box(outerRect, GUIContent.none);

        if (clip == null)
        {
            EditorGUI.LabelField(outerRect, "No Clip", GetCenteredLabelStyle());
            return;
        }

        float clampedTime = Mathf.Clamp(previewTime, 0f, duration);
        Sprite sprite = GetSpriteAtTime(clip, clampedTime);

        Rect textureRect = new Rect(
            outerRect.x + 8f,
            outerRect.y + 8f,
            outerRect.width - 16f,
            outerRect.height - 34f
        );

        if (sprite != null)
        {
            DrawSpritePreview(textureRect, sprite);
        }
        else
        {
            EditorGUI.DrawRect(textureRect, new Color(0.18f, 0.18f, 0.18f, 1f));
            EditorGUI.LabelField(textureRect, "No Sprite At This Time", GetCenteredLabelStyle());
        }

        Rect infoRect = new Rect(
            outerRect.x + 8f,
            outerRect.yMax - 22f,
            outerRect.width - 16f,
            18f
        );

        GUIStyle infoStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };

        string spriteName = sprite != null ? sprite.name : "None";
        EditorGUI.LabelField(infoRect, $"Time: {clampedTime:0.000}s    Sprite: {spriteName}", infoStyle);
    }

    private void DrawSpritePreview(Rect rect, Sprite sprite)
    {
        if (sprite == null || sprite.texture == null)
        {
            EditorGUI.LabelField(rect, "Invalid Sprite", GetCenteredLabelStyle());
            return;
        }

        EditorGUI.DrawRect(rect, new Color(0.13f, 0.13f, 0.13f, 1f));

        Texture2D tex = sprite.texture;

        Rect texCoords = new Rect(
            sprite.rect.x / tex.width,
            sprite.rect.y / tex.height,
            sprite.rect.width / tex.width,
            sprite.rect.height / tex.height
        );

        float spriteAspect = sprite.rect.width / sprite.rect.height;
        float rectAspect = rect.width / rect.height;

        Rect drawRect = rect;

        if (spriteAspect > rectAspect)
        {
            float h = rect.width / spriteAspect;
            drawRect.y += (rect.height - h) * 0.5f;
            drawRect.height = h;
        }
        else
        {
            float w = rect.height * spriteAspect;
            drawRect.x += (rect.width - w) * 0.5f;
            drawRect.width = w;
        }

        GUI.DrawTextureWithTexCoords(drawRect, tex, texCoords);
    }

    private GUIStyle GetCenteredLabelStyle()
    {
        GUIStyle style = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 12
        };
        return style;
    }

    private Sprite GetSpriteAtTime(AnimationClip clip, float time)
    {
        if (clip == null)
            return null;

        var bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);

        EditorCurveBinding spriteBinding = default;
        bool found = false;

        for (int i = 0; i < bindings.Length; i++)
        {
            // SpriteRenderer 的 sprite 属性一般就是 m_Sprite
            if (bindings[i].propertyName == "m_Sprite")
            {
                spriteBinding = bindings[i];
                found = true;
                break;
            }
        }

        if (!found)
            return null;

        ObjectReferenceKeyframe[] frames = AnimationUtility.GetObjectReferenceCurve(clip, spriteBinding);
        if (frames == null || frames.Length == 0)
            return null;

        Sprite current = null;

        for (int i = 0; i < frames.Length; i++)
        {
            if (frames[i].time <= time)
                current = frames[i].value as Sprite;
            else
                break;
        }

        // 如果 time 比第一个 key 还小，就直接给第一个
        if (current == null)
            current = frames[0].value as Sprite;

        return current;
    }
    private void Reset()
    {
        if(clipProp == null) return;
        AnimationClip clip = clipProp.objectReferenceValue as AnimationClip;
        if (clip == null) return;

        float duration = clip.length;

        Undo.RecordObject(target, "Reset Timeline");

        // 清空
        eventsProp.ClearArray();
        rangesProp.ClearArray();

        // Events
        eventsProp.InsertArrayElementAtIndex(0);
        eventsProp.InsertArrayElementAtIndex(1);

        eventsProp.GetArrayElementAtIndex(0).FindPropertyRelative("name").stringValue = "EffectTime";
        eventsProp.GetArrayElementAtIndex(0).FindPropertyRelative("time").floatValue = duration * 0.5f;

        eventsProp.GetArrayElementAtIndex(1).FindPropertyRelative("name").stringValue = "CancelStart";
        eventsProp.GetArrayElementAtIndex(1).FindPropertyRelative("time").floatValue = duration * 0.75f;

        // Ranges
        rangesProp.InsertArrayElementAtIndex(0);
        rangesProp.InsertArrayElementAtIndex(1);

        rangesProp.GetArrayElementAtIndex(0).FindPropertyRelative("name").stringValue = "Windup";
        rangesProp.GetArrayElementAtIndex(0).FindPropertyRelative("start").floatValue = 0f;
        rangesProp.GetArrayElementAtIndex(0).FindPropertyRelative("end").floatValue = duration * 0.5f;

        rangesProp.GetArrayElementAtIndex(1).FindPropertyRelative("name").stringValue = "Recover";
        rangesProp.GetArrayElementAtIndex(1).FindPropertyRelative("start").floatValue = duration * 0.5f;
        rangesProp.GetArrayElementAtIndex(1).FindPropertyRelative("end").floatValue = duration;

        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(target);

        Repaint();
    }
}