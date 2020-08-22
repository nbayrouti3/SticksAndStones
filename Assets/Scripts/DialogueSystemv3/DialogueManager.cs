﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager instance;  //to make the class Singleton
    public static bool triggered;
    public static bool spokenTo = false;

    public DialogueBase next; //this should be set when returning to base dialogue after completing an option branch
    public DialogueBase learningDialogue;


    private bool typing;
    private string completeText;

    private bool isitemDialogue = false;

    private bool isLockedDoorDialogue = false;

    //This is just making sure this is the class being referenced by DialogueManager
    private void Awake()
    {
        if (instance != null)
        {
            Destroy(this);
        }
        else
        {
            instance = this;
        }
        DontDestroyOnLoad(this);
    }

    private void Update()
    {
        if (triggered)
        {
            if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Space))
            {
                NextLine();
            }
        }
    }

    public GameObject display;
    public Text dialogueName;
    public GameObject nameTag;
    public Text dialogueText;
    public Image dialoguePortrait;
    public float delay = 0.01f;
    private int numOptions;

    public Queue<DialogueBase.Info> dialogueInfo = new Queue<DialogueBase.Info>();

    public bool isDialogueOption = false;
    public GameObject optionUI;
    public GameObject[] optionButtons;
    public GameObject dialogueUI;
    private bool buffer;
    [HideInInspector] public bool abilityLearned = false;

    public List<DialogueBase.Info> previousDialoguesInfo;

    public void AddDialogue(DialogueBase db)
    {
         DialogueBase.Info firstDialogue = db.dialogueInfo[0];
        if (triggered)
        {
            return;
        }



        StartCoroutine(Buffer()); //required so that the first text to appear types instead of just appearing

        triggered = true;
        dialogueInfo.Clear();  //makes sure the queue is empty before queuing new dialogue
        display.SetActive(true);  //UI updates
        dialogueUI.SetActive(true);   //UI updates

        ParseOptions(db); //done before enqueuing the dialogue so the options actually display

        foreach (DialogueBase.Info info in db.dialogueInfo)
        {
            dialogueInfo.Enqueue(info);
        }

        DequeueDialogue(); //start displaying the dialogue
    }

    public void DequeueDialogue()
    {
        //the following if ensures that text doesn't get jumbled if you try to skip through quickly
        if (typing)
        {
            if (buffer) return;
            CompleteText();
            StopAllCoroutines();
            typing = false;
            return;
        }

        if (dialogueInfo.Count == 0)
        {
            EndDialogue();
            return;
        }

        DialogueBase.Info info = dialogueInfo.Dequeue();
        //Checks for one time dialogue
        if (info.oneTime) {
            isitemDialogue = true;
            if (previousDialoguesInfo.Contains(info)) {
                EndDialogue();
                return;
            } else {
                previousDialoguesInfo.Add(info);
            }
        }
        completeText = info.words;
        dialogueText.text = info.words;
        dialoguePortrait.sprite = info.portrait;

        //Adds abilities to learning system
        if (info.abilityToLearn != null) 
        {
            if (info.isAttack) 
            {
                LearningSystem.instance.AddAttack(info.abilityToLearn);
            } else if (info.isSkill) 
            {
                LearningSystem.instance.AddSkill(info.abilityToLearn);
            } else {
                Debug.Log("Please set " + info.abilityToLearn.name + " to be a skill or attack!");
            }
        }

        //Checks and changes will related stuff
        if (info.affectsWill) 
        {
            PlayerStats.Instance.adjustWill(info.willChangeAmount);
            Debug.Log("Will was changed!");
        }
        //Checks and changes anxiety related stuff
        if (info.affectsAnxiety)
        {
            PlayerStats.Instance.adjustAnxiety(info.anxietyChangeAmount);
            Debug.Log("Anxiety was changed!");
        }

        // checks if locked door dialogue
        if (info.lockedDoor) {
            isLockedDoorDialogue = true;
        }

        //Checks to give items
        if (info.givesItems) {
            isitemDialogue = true;
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            InventoryManager inventoryManager = player.GetComponent<InventoryManager>();
            for (int i = 0; i < info.itemNumGiven; i++) {
                GameObject itemObject = Instantiate(info.itemGiven);
                itemObject.name = itemObject.name + i;
                inventoryManager.inventory.AddItem(itemObject.GetComponent<Item>().item, itemObject);
                Vector3 pos = player.transform.position;
                itemObject.transform.position = pos;
                itemObject.SetActive(false);
                DontDestroyOnLoad(itemObject);
            } 
        }

        //setting UI elements
        if (info.charName == "")
        {
            dialogueName.text = "";
            nameTag.SetActive(false);
        }
        else
        {
            dialogueName.text = info.charName;
            nameTag.SetActive(true);
        }

        //setting next dialogue if necessary
        if (info.nextDialogue != null)
        {
            next = info.nextDialogue;
        }
        else
        {
            next = null;
        }

        dialogueText.text = "";
        StartCoroutine(TypeText(info));
    }

    //Coroutine to type the text instead of having it just appear
    //Needs to be a coroutine otherwise the letters get jumbled
    IEnumerator TypeText(DialogueBase.Info info)
    {
        typing = true;
        dialogueText.text = "";
        foreach(char c in info.words.ToCharArray())
        {
            yield return new WaitForSeconds(delay);
            dialogueText.text += c;
            yield return null;
        }
        typing = false;
    }

    //for waiting purposes :)
    IEnumerator Buffer()
    {
        yield return new WaitForSeconds(0.1f);
        buffer = false;
    }

    //replaces whatever's in the dialogue box with the completed version of the text
    private void CompleteText()
    {
        dialogueText.text = completeText;
    }

    public void NextLine()
    {
        DequeueDialogue();
    }

    public void EndDialogue()
    {
        if (!LearningSystem.instance.isAttacksEmpty()) 
        {
            LearningSystem.instance.LearnAttacks();
            abilityLearned = true;
        } else if (!LearningSystem.instance.isSkillsEmpty()) 
        {
            LearningSystem.instance.LearnSkills();
            abilityLearned = true;
        }
        OptionsLogic();
        if (isDialogueOption || next != null) {
            AddDialogue(next);  
            Debug.Log("triggered");
        } else {
            if (isLockedDoorDialogue) {
                dialogueUI.SetActive(false);
                isLockedDoorDialogue = false;
            } else {
                AudioManager.instance.Play(0);
                dialogueUI.SetActive(false);
                triggered = false;
                if (!isitemDialogue) {
                    TimeProgression.Instance.ChangeTime();
                    spokenTo = true;
                } else {
                    isitemDialogue = false;
                    spokenTo = false;
                }
            }
        }
    }

    //displays options if available or closes dialogue (latter doesn't matter if next dialogue is queued up)
    private void OptionsLogic()
    {
        if (isDialogueOption)
        {
            optionUI.SetActive(true);
            dialogueUI.SetActive(false);
        }
        else
        {
            display.SetActive(false);
            triggered = false;
        }

    }

    //pure UI update
    public void CloseOptions()
    {
        optionUI.SetActive(false);
    }

    private void ParseOptions(DialogueBase db)
    {
        if (db is DialogueOptions)
        {
            isDialogueOption = true;
            DialogueOptions dialogueOptions = db as DialogueOptions;
            numOptions = dialogueOptions.optionInfo.Length;

            optionButtons[0].GetComponent<Button>().Select(); //has the first button automatically selected (won't be highlighted until you move the cursor)


            for (int i = 0; i < optionButtons.Length; i++)
            {
                optionButtons[i].SetActive(false); //makes sure no buttons are showing
            }

            for (int i = 0; i < numOptions; i++)
            {
                optionButtons[i].SetActive(true); //activates only the required number of options
                optionButtons[i].transform.GetChild(0).gameObject.GetComponent<Text>().text = dialogueOptions.optionInfo[i].buttonName; //sets button names
                UnityEventHandler handler = optionButtons[i].GetComponent<UnityEventHandler>(); //allows access to button trigger to set custom event
                handler.eventHandler = dialogueOptions.optionInfo[i].myEvent; //hooks up the UnityEventHandler to the button's events and triggers

                if (dialogueOptions.optionInfo[i].nextDialogue != null)
                {
                    handler.dialogue = dialogueOptions.optionInfo[i].nextDialogue; //displays option branch dialogue if available
                }
                else
                {
                    handler.dialogue = null; //needs to be null for UnityEventHandler to know what to do with it
                }
            }
        }
        else
        {
            isDialogueOption = false;
        }
    }

    public void LearnAbilities() {
        learningDialogue.dialogueInfo[0].words = "From this conversation you feel like you've learned:" + LearningSystem.instance.toString();
        abilityLearned = false;
        LearningSystem.instance.clearLists();
        AddDialogue(learningDialogue);
        Debug.Log("learning dialogue activated");
    }
}
