﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Dialogue", menuName = "Dialogues")]
public class DialogueBase : ScriptableObject
{
    [System.Serializable]
    public class Info
    {
        public DialogueBase nextDialogue;
        public string charName;
        public Sprite portrait;
        [TextArea(4, 10)]
        public string words;

        public Info(string msg)
        {
            nextDialogue = null;
            charName = "";
            Sprite portrait = null;
            words = msg;
        }

        //public Action toLearn;    need to double check how to do these
        public PlayerAbility abilityToLearn;
        public bool isAttack; //Check this if it is an attack
        public bool isSkill; //Check this if it is a skill

    }

    [Header("Insert dialogue info below")]
    public Info[] dialogueInfo;

    public void init(string msg)
    {
        new Info(msg);
    }

    //DialogueBase newDialogue = ScriptableObject.CreateInstance(typeof(DialogueBase)) as DialogueBase;

    /*public DialogueBase(string msg)
    {
        new Info(msg);
    }*/
}
