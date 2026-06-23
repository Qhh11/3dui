using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class WaterRescueController : MonoBehaviour
{
    public static WaterRescueController Instance
    {
        get;
        private set;
    }

    private DialogueSequence rescueSequence;

    private PlayerController activePlayer;
    private Coroutine waitForDialogueRoutine;

    private bool rescueInProgress;

    public bool RescueInProgress => rescueInProgress;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        BuildRescueDialogue();
    }

    private void OnDestroy()
    {
        if (activePlayer != null)
        {
            activePlayer.enabled = true;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// 在运行时创建一句落水对白。
    /// 使用现有 Player 的名称、头像和颜色。
    /// </summary>
    private void BuildRescueDialogue()
    {
        if (rescueSequence != null)
        {
            return;
        }

        Level1DialogueDatabase database =
            Level1DialogueDatabase.Instance;

        if (database == null)
        {
            Debug.LogWarning(
                "WaterRescueController 找不到 " +
                "Level1DialogueDatabase。",
                this
            );

            return;
        }

        DialogueCharacter playerCharacter =
            database.GetCharacter(
                DialogueIds.Characters.Player
            );

        if (playerCharacter == null)
        {
            Debug.LogWarning(
                "WaterRescueController 找不到 Player 对话角色。",
                this
            );

            return;
        }

        rescueSequence =
            ScriptableObject.CreateInstance
                <DialogueSequence>();

        rescueSequence.sequenceId =
            "water_rescue";

        rescueSequence.entries =
            new List<DialogueEntry>
            {
                new DialogueEntry
                {
                    speaker = playerCharacter,

                    text =
                        "I'm in the water! " +
                        "UFO, get me back to shore!",

                    eventOnStart = "",
                    eventOnEnd = "",

                    choices =
                        new List<DialogueChoice>()
                }
            };
    }

    /// <summary>
    /// 由 WaterKillZone 调用。
    /// </summary>
    public void RequestRescue(
        PlayerController player)
    {
        if (player == null || rescueInProgress)
        {
            return;
        }

        DialogueRunner dialogueRunner =
            DialogueRunner.Instance;

        /*
         * 如果其他剧情 Dialogue 正在播放，
         * 不覆盖当前剧情，直接把玩家送回检查点。
         */
        if (dialogueRunner != null &&
            dialogueRunner.IsDialogueOpen)
        {
            player.Respawn();
            return;
        }

        BuildRescueDialogue();

        rescueInProgress = true;
        activePlayer = player;

        // 玩家停在水中，不能移动，也不会继续下坠
        activePlayer.enabled = false;

        /*
         * Dialogue 系统缺失时直接重生，
         * 避免玩家永远卡在水中。
         */
        if (dialogueRunner == null ||
            rescueSequence == null)
        {
            CompleteRescue();
            return;
        }

        dialogueRunner.StartDialogue(
            rescueSequence
        );

        waitForDialogueRoutine =
            StartCoroutine(
                WaitForDialogueToClose(
                    dialogueRunner
                )
            );
    }

    /// <summary>
    /// 等待玩家点击并关闭这一句 Dialogue。
    /// Dialogue 关闭后立即重生。
    /// </summary>
    private IEnumerator WaitForDialogueToClose(
        DialogueRunner dialogueRunner)
    {
        while (dialogueRunner != null &&
               dialogueRunner.IsDialogueOpen)
        {
            yield return null;
        }

        waitForDialogueRoutine = null;
        CompleteRescue();
    }

    private void CompleteRescue()
    {
        if (activePlayer != null)
        {
            activePlayer.Respawn();
            activePlayer.enabled = true;
        }

        activePlayer = null;
        rescueInProgress = false;
    }
}