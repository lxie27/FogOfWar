using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField]
    Toggle showServerView;
    [SerializeField]
    Toggle wallhacks;

    [SerializeField]
    Player1 player1;
    [SerializeField]
    GameObject player2Shade;
    [SerializeField]
    SpriteRenderer p2SpriteRenderer;

    private Canvas p2Canvas;
    private SpriteRenderer[] p2ChildSpriteRenderers;
    private SpriteRenderer p2ShadeSpriteRenderer;
    private SpriteRenderer[] p2ShadeChildSpriteRenderers;

    private bool lastShowServerViewState;
    private bool lastWallhacksState;

    void Start()
    {
        lastShowServerViewState = showServerView.isOn;
        lastWallhacksState = wallhacks.isOn;
        p2Canvas = p2SpriteRenderer.gameObject.GetComponentInChildren<Canvas>();
        p2ChildSpriteRenderers = p2SpriteRenderer.gameObject.GetComponentsInChildren<SpriteRenderer>();

        p2ShadeSpriteRenderer = player2Shade.GetComponent<SpriteRenderer>();
        p2ShadeChildSpriteRenderers = player2Shade.gameObject.GetComponentsInChildren<SpriteRenderer>();
    }

    void Update()
    {
        player2Shade.transform.position = player1.targetPosition;
        // toggles are mutually exclusive
        if (showServerView.isOn != lastShowServerViewState)
        {
            if (showServerView.isOn && wallhacks.isOn)
            {
                wallhacks.isOn = false;
            }
            lastShowServerViewState = showServerView.isOn;
        }
        if (wallhacks.isOn != lastWallhacksState)
        {
            if (wallhacks.isOn && showServerView.isOn)
            {
                showServerView.isOn = false;
            }
            lastWallhacksState = wallhacks.isOn;
        }

        if (wallhacks.isOn)
        {
            EnableWallhacks();
        }
        else if (showServerView.isOn)
        {
            EnableServerView();
        }
        else
        {
            DefaultView();
        }
    }
    void EnableWallhacks()
    {
        DefaultView();
        player1.wallhacksOn = true;
    }
    void EnableServerView()
    {
        foreach (var spriteRenderer in p2ChildSpriteRenderers)
        {
            spriteRenderer.enabled = true;
        }
        foreach (var spriteRenderer in p2ShadeChildSpriteRenderers)
        {
            spriteRenderer.enabled = false;
        }
        p2SpriteRenderer.enabled = true;
        p2ShadeSpriteRenderer.enabled = false;
        p2Canvas.enabled = true;
        player1.wallhacksOn = false;
    }

    void DefaultView()
    {
        foreach (var spriteRenderer in p2ChildSpriteRenderers)
        {
            spriteRenderer.enabled = false;
        }
        foreach (var spriteRenderer in p2ShadeChildSpriteRenderers)
        {
            spriteRenderer.enabled = player1.canSeeP2;
        }
        p2SpriteRenderer.enabled = false;
        p2ShadeSpriteRenderer.enabled = player1.canSeeP2;
        p2Canvas.enabled = false;
        player1.wallhacksOn = false;
    }
}
