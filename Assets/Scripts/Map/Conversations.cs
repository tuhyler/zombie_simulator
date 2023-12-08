using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Conversations : MonoBehaviour
{
    public static Conversations Instance { get; private set; }
    public Dictionary<string, List<ConversationItem>> conversationDict = new();

    public Sprite koaHappy, koaThinking, koaSurprised, koaSerious, koaConfused, koaEvil, azaiSerious, laborerHappy;

	private void Awake()
	{
        Instance = this;

		//doing this here instead of in inspector so I can search text more easily

		#region just_landed
		conversationDict["just_landed"] = new();

		ConversationItem just_landed1 = new()
		{
			speakerImage = koaHappy,
			speakerName = "Koa",
			speakerDirection = "Camera",
			speakerText = "Um... ow! Did anyone see that? I just fell out of the sky!"
		};
		conversationDict["just_landed"].Add(just_landed1);

		ConversationItem just_landed2 = new()
		{
			speakerImage = koaHappy,
			speakerName = "Koa",
			speakerDirection = "Camera",
			speakerText = "Hmm... I have no memory of how I got here, the last thing I remember is being on board the SS Comet...I was working in my holo-garden..." +
			"and now I'm here!"
		};
		conversationDict["just_landed"].Add(just_landed2);

		ConversationItem just_landed3 = new()
		{
			speakerImage = koaHappy,
			speakerName = "Koa",
			speakerDirection = "Camera",
			speakerText = "I'll figure out how to get back home in a second, but first I need to build a fire, make a shelter, and get some food."
		};
		conversationDict["just_landed"].Add(just_landed3);

		#endregion
		#region tutorial1
		conversationDict["tutorial1"] = new();

		ConversationItem tutorial11 = new()
		{
			speakerImage = koaHappy,
			speakerName = "Koa",
			speakerDirection = "Camera",
			speakerText = "Ok great! That will keep me warm for like a few seconds, but now I need some lumber for some shelter."
		};
		conversationDict["tutorial1"].Add(tutorial11);
		#endregion
		#region tutorial2
		conversationDict["tutorial2"] = new();

		ConversationItem tutorial21 = new()
		{
			speakerImage = koaHappy,
			speakerName = "Koa",
			speakerDirection = "Camera",
			speakerText = "Perfect, I have all the materials I need, now to build."
		};
		conversationDict["tutorial2"].Add(tutorial21);
		#endregion
		#region tutorial3
		conversationDict["tutorial3"] = new();

		ConversationItem tutorial31 = new()
		{
			speakerImage = koaHappy,
			speakerName = "Koa",
			speakerDirection = "Camera",
			speakerText = "Right, now I just need to go get some food, I probably have to harvest it myself..."
		};
		conversationDict["tutorial3"].Add(tutorial31);
		#endregion
		#region first_labor
		conversationDict["first_labor"] = new();

		ConversationItem first_labor1 = new()
		{
			speakerImage = koaHappy,
			speakerName = "Koa",
			speakerDirection = "Scott",
			speakerText = "Oh hello, who are you?"
		};
		conversationDict["first_labor"].Add(first_labor1);

		ConversationItem first_labor2 = new()
		{
			speakerImage = laborerHappy,
			speakerName = "Scott",
			speakerDirection = "Koa",
			speakerText = "Hi! I'm Scott! Isn't it a glorious day today?"
		};
		conversationDict["first_labor"].Add(first_labor2);

		ConversationItem first_labor3 = new()
		{
			speakerImage = koaHappy,
			speakerName = "Koa",
			speakerDirection = "Scott",
			speakerText = "Um, yes it's fine. Where did you come from?"
		};
		conversationDict["first_labor"].Add(first_labor3);

		ConversationItem first_labor4 = new()
		{
			speakerImage = laborerHappy,
			speakerName = "Scott",
			speakerDirection = "Koa",
			speakerText = "No clue! But I'm glad to be here and happy to help!"
		};
		conversationDict["first_labor"].Add(first_labor4);

		ConversationItem first_labor5 = new()
		{
			speakerImage = koaSerious,
			speakerName = "Koa",
			speakerDirection = "Scott",
			speakerText = "So I'm guessing you showed up when I gathered some food. Say, could you help me get home?"
		};
		conversationDict["first_labor"].Add(first_labor5);

		ConversationItem first_labor6 = new()
		{
			speakerImage = laborerHappy,
			speakerName = "Scott",
			speakerDirection = "Koa",
			speakerText = "Absolutely I will! Do you live right around the corner?"
		};
		conversationDict["first_labor"].Add(first_labor6);

		ConversationItem first_labor7 = new()
		{
			speakerImage = koaSerious,
			speakerName = "Koa",
			speakerDirection = "Scott",
			speakerText = "Well, no, it's actually up in space... uh, this is kind of a big ask, but could you help me build a rocket?"
		};
		conversationDict["first_labor"].Add(first_labor7);

		ConversationItem first_labor8 = new()
		{
			speakerImage = laborerHappy,
			speakerName = "Scott",
			speakerDirection = "Koa",
			speakerText = "Sure thing!! What do I need to do? Also, and this probably isn't important, but what's a rocket? Is it like rubbing sticks together? I'm great at that!"
		};
		conversationDict["first_labor"].Add(first_labor8);

		ConversationItem first_labor9 = new()
		{
			speakerImage = laborerHappy,
			speakerName = "Scott",
			speakerDirection = "Koa",
			speakerText = "We don't have a lot of know-how around here, but we have lots of materials to build stuff!"
		};
		conversationDict["first_labor"].Add(first_labor9);

		ConversationItem first_labor10 = new()
		{
			speakerImage = koaEvil,
			speakerName = "Koa",
			speakerDirection = "Camera",
			speakerText = "*quietly to himself* hmm, if I get more food, then I can get more of these people and get them to gather all the material I need..."
		};
		conversationDict["first_labor"].Add(first_labor10);

		ConversationItem first_labor11 = new()
		{
			speakerImage = laborerHappy,
			speakerName = "Scott",
			speakerDirection = "Koa",
			speakerText = "*leaning in, clearly eavesdropping* Sounds like fun! Just make sure we have housing and water and we'll gladly stick around to help!"
		};
		conversationDict["first_labor"].Add(first_labor11);

		ConversationItem first_labor12 = new()
		{
			speakerImage = koaHappy,
			speakerName = "Koa",
			speakerDirection = "Scott",
			speakerText = "You heard that? Well then, uh, I'm going to go get some supplies to build a farm you can work on."
		};
		conversationDict["first_labor"].Add(first_labor12);
		#endregion
	}
}

[Serializable]
public struct ConversationItem
{
    public Sprite speakerImage;
    public string speakerName;
	public string speakerDirection;
    public string speakerText;
}
