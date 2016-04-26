using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CharacterRelations
{
    //Relations

    Character myself;

    public Character father                 { get; protected set; }
    public Character mother                 { get; protected set; }
    public Character spouce                 { get; protected set; }
    public List<Character> siblings         { get; protected set; }
    public List<Character> children         { get; protected set; }
    public List<Character> acquaintances    { get; protected set; }

    public CharacterRelations(Character myself, Character father, Character mother)
    {
        bool error = false; // Temp until we figure out how to tell the character he has no parents.
        if (myself == null)
        {
            Debug.LogError("CharacterRelations: Character thinks he does not exist (myself = null)");
            error = true;
        }

        if (father == null)
        {
            Debug.LogError("CharacterRelations: Character has no father :'(");
            error = true;
        }

        if (mother == null)
        {
            Debug.LogError("CharacterRelations: Character has no mother :'(");
            error = true;
        }

        if (!error)
        {
            //Know Thyself
            this.myself = myself;

            //Set mother and father
            this.father = father;
            this.mother = mother;

            //Set siblings
            siblings.AddRange(father.myCharacterRelations.children);
            siblings.Union(mother.myCharacterRelations.children);

            //Tell mother and father they have a new child.
            father.myCharacterRelations.addChild(myself);
            mother.myCharacterRelations.addChild(myself);

            //Tell any siblings they will have to share their toys
            foreach (Character c in siblings)
            {
                c.myCharacterRelations.addSibling(myself);
            }

            //New people do not know anyone else (replace with knowing random people that mother/father/siblings know) TODO
            //acquaintances.Add(null);
        }
    }

    void addChild(Character c)
    {
        children.Add(c);
    }

    void addSibling(Character c)
    {
        siblings.Add(c);
    }

}

public struct CharacterDetails 
{
    //Name
    public string myName;

    //Sex Male = true / Female = false
    public bool mySex;

    //Age
    public int myAge;

    //Birthplace
    public string myBirthplace;

    //Alive? 
    public bool isAlive;

    public CharacterDetails(string name, bool sex, int age, string birthplace, bool alive)
    {
        myName = name;
        mySex = sex;
        myAge = age;
        myBirthplace = birthplace;
        isAlive = alive;
    }
}



public class CharacterSkills
{
    Dictionary<string, CharacterSkill> mySkills;

    public CharacterSkills()
    {
        loadSkillsFromXMLFile();
    }

    void loadSkillsFromXMLFile()
    {
        //TODO read from xml

        mySkills.Add("Farming", new CharacterSkill("Farming", 1, 500, CharacterSkill.Affinity.Natural));
        mySkills.Add("Lifting", new CharacterSkill("Lifting", 2, 1500, CharacterSkill.Affinity.None));
        mySkills.Add("Building", new CharacterSkill("Building", 3, 2200, CharacterSkill.Affinity.Passionate));
    }

    public void addXP(string skillName, int amt)
    {
        if (mySkills.ContainsKey(skillName))
        {
            mySkills[skillName].addXP(amt);
        }

        else
        {
            Debug.LogError("CharacterSpecifics: addXP: trying to add xp to non-existant skill");
        }
    }
}

//Can be farmed out to xml files
public class CharacterSkill
{
    public enum Affinity { None, Natural, Passionate };
    int[] xpTable = { 1000, 2000, 3000, 4000, 5000, 6000, 7000 }; 

    string skillName;
    int currentLevel;
    int currentXP;
    Affinity affinity;


    public CharacterSkill(string skillName, int currentLevel, int currentXP, Affinity affinity)
    {
        this.skillName = skillName;
        this.currentLevel = currentLevel;
        this.currentXP = currentXP;
        this.affinity = affinity;
    }

    public void addXP(int amt)
    {
        currentXP += amt;
        checkForLevelUp();
    }

    void checkForLevelUp()
    {
        if (currentLevel - 1 < xpTable.Length)
        {
            if (currentXP >= xpTable[currentLevel])
            {
                currentLevel++;
                currentXP = 0;
            }
        }
    }
}