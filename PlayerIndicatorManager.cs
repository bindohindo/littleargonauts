using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerIndicatorManager : MonoBehaviour
{
    Canvas canvas;
    RectTransform canvasRect;

    [SerializeField] List<PlayerIndicator> indicators = new List<PlayerIndicator>();
    [SerializeField] List<GameObject> players = new List<GameObject>(); 
    [SerializeField] float indicatorMinSizeDistance = 3000f;

    Dictionary<int, PlayerIndicator> playerIndicators = new Dictionary<int, PlayerIndicator>();

    private void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        canvasRect = canvas.GetComponent<RectTransform>();

        indicators = new List<PlayerIndicator>(GetComponentsInChildren<PlayerIndicator>());
    }

    private void Start()
    {
        GameManager.Singleton.OnRegisterPlayer += RegisterPlayer;
        GameManager.Singleton.OnUnregisterPlayer += UnregisterPlayer;
    }

    private void OnEnable()
    {
        UIManager.Singleton.OnUpdateIndicatorColors += UpdateIndicatorColors;
    }

    private void OnDisable()
    {
        GameManager.Singleton.OnRegisterPlayer -= RegisterPlayer;
        GameManager.Singleton.OnUnregisterPlayer -= UnregisterPlayer;
        UIManager.Singleton.OnUpdateIndicatorColors -= UpdateIndicatorColors;

        playerIndicators.Clear();
    }

    public void RegisterPlayer(GameObject playerObj)
    {
        var player = playerObj.GetComponent<PlayerManager>();
        int index = GameManager.Singleton.ActivePlayers.IndexOf(playerObj);

        players.Add(playerObj);
        playerIndicators.Add(player.playerId, indicators[index]);

        indicators[index].name = "Player" + player.playerId + "_Indicator";
        indicators[index].indicatorImage.color = playerObj.GetComponent<MeshRenderer>().material.color;
    }

    public void UnregisterPlayer(GameObject playerObj)
    {
        playerIndicators.Remove(playerObj.GetComponent<PlayerManager>().playerId);
        players.Remove(playerObj);
    }

    private void UpdateIndicatorColors(GameObject playerObj, Color next)
    {
        var player = playerObj.GetComponent<PlayerManager>();
        playerIndicators[player.playerId].indicatorImage.color = next;
    }

    public void IndicatePlayerOutOfView()
    {
        foreach (var playerObj in players)
        {
            var player = playerObj.GetComponent<PlayerManager>();
            Vector3 playerPos = player.transform.position;
            playerPos.y = TargetGroup.Singleton.transform.position.y;

            Vector3 indicatorScreenPos = Camera.main.WorldToScreenPoint(playerPos);
            Vector3 playerScreenPos = indicatorScreenPos;

            if (playerScreenPos.x < 0f || playerScreenPos.x > canvasRect.rect.width || playerScreenPos.y < 0f || playerScreenPos.y > canvasRect.rect.height)
            {
                player.isVisible = false;
            }
            else
            {
                player.isVisible = true;
                playerIndicators[player.playerId].gameObject.SetActive(false);
            }

            if (player.isVisible == false)
            {
                if (indicatorScreenPos.z < 0f)
                {
                    indicatorScreenPos = -indicatorScreenPos;
                }

                Vector2 screenCenter = new Vector2(canvasRect.rect.width / 2f, canvasRect.rect.height / 2f);
                float distanceFromCenter = Vector2.Distance(screenCenter, playerScreenPos);

                Vector2 maxRect = playerIndicators[player.playerId].maxRectSize;
                Vector2 minRect = playerIndicators[player.playerId].minRectSize;

                playerIndicators[player.playerId].currentRectSize = Vector2.Lerp(maxRect, minRect, distanceFromCenter / indicatorMinSizeDistance);

                playerIndicators[player.playerId].rectTransform.SetSizeWithCurrentAnchors
                    (RectTransform.Axis.Horizontal, playerIndicators[player.playerId].currentRectSize.x);
                playerIndicators[player.playerId].rectTransform.SetSizeWithCurrentAnchors
                    (RectTransform.Axis.Vertical, playerIndicators[player.playerId].currentRectSize.y);

                float indicatorOffsetX = playerIndicators[player.playerId].currentRectSize.x / 2f;
                float indicatorOffsetY = playerIndicators[player.playerId].currentRectSize.y / 2f;

                indicatorScreenPos.x = Mathf.Clamp(indicatorScreenPos.x, 0f + indicatorOffsetX, canvasRect.rect.width - indicatorOffsetX);
                indicatorScreenPos.y = Mathf.Clamp(indicatorScreenPos.y, 0f + indicatorOffsetY, canvasRect.rect.height - indicatorOffsetY);

                playerIndicators[player.playerId].transform.position = indicatorScreenPos;

                playerIndicators[player.playerId].gameObject.SetActive(true);
            }
        }
    }
}
