using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class UIFixProxyClick : MonoBehaviour
{
    public Canvas uiCanvas;
    public Camera uiCamera;

    private GraphicRaycaster _raycaster;
    private PointerEventData _pointer;
    private List<RaycastResult> _results;

    void Awake()
    {
        if (!uiCanvas) uiCanvas = GetComponent<Canvas>();
        _raycaster = uiCanvas.GetComponent<GraphicRaycaster>();
        _pointer = new PointerEventData(EventSystem.current);
        _results = new List<RaycastResult>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            _pointer.position = Input.mousePosition;
            _results.Clear();
            _raycaster.Raycast(_pointer, _results);
            if (_results.Count > 0)
            {
                var go = _results[0].gameObject;
                // Пробуем вызвать событие клика вручную
                ExecuteEvents.Execute(go, _pointer, ExecuteEvents.pointerClickHandler);
            }
        }
    }
}
