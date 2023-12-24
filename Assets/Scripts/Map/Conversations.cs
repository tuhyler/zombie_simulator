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
			speakerText = "Um... ow! I just fell out of the sky!"
		};
		conversationDict["just_landed"].Add(just_landed1);

		ConversationItem just_landed2 = new()
		{
			speakerImage = koaHappy,
			speakerName = "Koa",
			speakerDirection = "Camera",
			speakerText = "How did I get here? The last thing I remember is going to sleep on the USS Comet... and now I'm here!"
		};
		conversationDict["just_landed"].Add(just_landed2);

		ConversationItem just_landed3 = new()
		{
			speakerImage = koaHappy,
			speakerName = "Koa",
			speakerDirection = "Camera",
			speakerText = "And it's cold out here! I guess I'll build a fire, maybe people will see the smoke?"
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
			speakerText = "Ok great! That will keep me warm for like a few seconds, but it could start raining soon... I need to find some lumber for some shelter."
		};
		conversationDict["tutorial1"].Add(tutorial11);

		ConversationItem tutorial12 = new()
		{
			speakerImage = koaHappy,
			speakerName = "Koa",
			speakerDirection = "Camera",
			speakerText = "(Explore the world by moving the camera around using the arrow or WASD keys. Use Q and E to rotate.)",
			action = true
		};
		conversationDict["tutorial1"].Add(tutorial12);

		ConversationItem tutorial13 = new()
		{
			speakerImage = koaHappy,
			speakerName = "Koa",
			speakerDirection = "Camera",
			speakerText = "(Move to the highlighted tile by selecting the button on the right or by right-clicking on the tile.)",
			action = true
		};
		conversationDict["tutorial1"].Add(tutorial13);
		#endregion
		#region tutorial2
		conversationDict["tutorial2"] = new();

		ConversationItem tutorial21 = new()
		{
			speakerImage = koaHappy,
			speakerName = "Koa",
			speakerDirection = "Camera",
			speakerText = "OK, here's a forest, now I just need to gather some lumber."
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
			speakerText = "OK, that's one lumber down... Now I need to get 4 more lumber to build a hut."
		};
		conversationDict["tutorial3"].Add(tutorial31);
		#endregion
		#region tutorial4
		conversationDict["tutorial4"] = new();

		ConversationItem tutorial41 = new()
		{
			speakerImage = koaHappy,
			speakerName = "Koa",
			speakerDirection = "Camera",
			speakerText = "Ugh... that was tedious, but that should be enough! I'll need to go to the camp now and build the hut."
		};
		conversationDict["tutorial4"].Add(tutorial41);

		ConversationItem tutorial42 = new()
		{
			speakerImage = koaHappy,
			speakerName = "Koa",
			speakerDirection = "Camera",
			speakerText = "(Click on the camp to perform actions within the camp.)",
			action = true
		};
		conversationDict["tutorial4"].Add(tutorial42);
		#endregion
		#region tutorial5
		conversationDict["tutorial5"] = new();

		ConversationItem tutorial51 = new()
		{
			speakerImage = koaHappy,
			speakerName = "Koa",
			speakerDirection = "Camera",
			speakerText = "K, here's a hut, it's a little small...but that's okay, I won't be here long. Now to get some food."
		};
		conversationDict["tutorial5"].Add(tutorial51);
		#endregion
		#region tutorial6
		conversationDict["tutorial6"] = new();

		ConversationItem tutorial61 = new()
		{
			speakerImage = koaHappy,
			speakerName = "Koa",
			speakerDirection = "Camera",
			speakerText = "This is an excellent place for food, seems all the locations with long grass have resources to harvest. " +
			"Let's get some food."
		};
		conversationDict["tutorial6"].Add(tutorial61);
		#endregion
		#region tutorial7
		conversationDict["tutorial7"] = new();

		ConversationItem tutorial71 = new()
		{
			speakerImage = koaHappy,
			speakerName = "Koa",
			speakerDirection = "Camera",
			speakerText = "Who's that guy?"
		};
		conversationDict["tutorial7"].Add(tutorial71);

		ConversationItem tutorial72 = new()
		{
			speakerImage = koaHappy,
			speakerName = "Koa",
			speakerDirection = "Camera",
			speakerText = "(Walk over to someone to talk to them if they have something to say)"
		};
		conversationDict["tutorial7"].Add(tutorial72);
		#endregion
		#region first_labor
		conversationDict["first_labor"] = new();

		ConversationItem first_labor1 = new()
		{
			speakerImage = koaHappy,
			speakerName = "Koa",
			speakerDirection = "Scott",
			speakerText = "Oh hello... who are you?"
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
			speakerText = "Um, yes, it's fine. Where did you come from?"
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
			speakerText = "So I'm guessing you showed up when you saw I had some food. Say... could you help me get home?"
		};
		conversationDict["first_labor"].Add(first_labor5);

		ConversationItem first_labor6 = new()
		{
			speakerImage = laborerHappy,
			speakerName = "Scott",
			speakerDirection = "Koa",
			speakerText = "Sure thing! Do you live right around the corner?"
		};
		conversationDict["first_labor"].Add(first_labor6);

		ConversationItem first_labor7 = new()
		{
			speakerImage = koaSerious,
			speakerName = "Koa",
			speakerDirection = "Scott",
			speakerText = "Well, no, it's actually not on this planet... uh, this is kind of a big ask, but could you help me build a rocket?"
		};
		conversationDict["first_labor"].Add(first_labor7);

		ConversationItem first_labor8 = new()
		{
			speakerImage = laborerHappy,
			speakerName = "Scott",
			speakerDirection = "Koa",
			speakerText = "Absolutely I will! What do I need to do? Also, and this probably isn't important, but what's a rocket? Is it like rubbing sticks together? I'm great at that!"
		};
		conversationDict["first_labor"].Add(first_labor8);

		ConversationItem first_labor9 = new()
		{
			speakerImage = koaHappy,
			speakerName = "Koa",
			speakerDirection = "Scott",
			speakerText = "Um, I guess it's like a tall, metal cylinder? And it uses fuel to get off the ground?"
		};
		conversationDict["first_labor"].Add(first_labor9);

		ConversationItem first_labor10 = new()
		{
			speakerImage = laborerHappy,
			speakerName = "Scott",
			speakerDirection = "Koa",
			speakerText = "You mean something that FLIES?! How does the fuel work?"
		};
		conversationDict["first_labor"].Add(first_labor10);

		ConversationItem first_labor11 = new()
		{
			speakerImage = koaHappy,
			speakerName = "Koa",
			speakerDirection = "Scott",
			speakerText = "Oh geez, uh, you know, I don't really know how it works... and you clearly don't know much about anything..."
		};
		conversationDict["first_labor"].Add(first_labor11);

		ConversationItem first_labor12 = new()
		{
			speakerImage = laborerHappy,
			speakerName = "Scott",
			speakerDirection = "Koa",
			speakerText = "*with a big smile* Nope!"
		};
		conversationDict["first_labor"].Add(first_labor12);

		ConversationItem first_labor13 = new()
		{
			speakerImage = laborerHappy,
			speakerName = "Scott",
			speakerDirection = "Koa",
			speakerText = "We don't have a lot of know-how around here, but if we get more people, we can all figure it out to get you what you need!"
		};
		conversationDict["first_labor"].Add(first_labor13);

		ConversationItem first_labor14 = new()
		{
			speakerImage = koaHappy,
			speakerName = "Koa",
			speakerDirection = "Scott",
			speakerText = "Really? You will help me? How do we get more people?"
		};
		conversationDict["first_labor"].Add(first_labor14);

		ConversationItem first_labor15 = new()
		{
			speakerImage = laborerHappy,
			speakerName = "Scott",
			speakerDirection = "Koa",
			speakerText = "Oh it's easy! Just provide food, housing, and water, and so long as you're here, they'll contribute!"
		};
		conversationDict["first_labor"].Add(first_labor15);

		ConversationItem first_labor16 = new()
		{
			speakerImage = laborerHappy,
			speakerName = "Scott",
			speakerDirection = "Koa",
			speakerText = "I can personally help you gather supplies and even build roads! In fact, let's get some more food."
		};
		conversationDict["first_labor"].Add(first_labor16);
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
	public bool action;
}
