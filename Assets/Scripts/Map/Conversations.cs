using System;
using System.Collections.Generic;
using UnityEngine;

public class Conversations : MonoBehaviour
{
    public static Conversations Instance { get; private set; }
    public Dictionary<string, List<ConversationItem>> conversationDict = new();

  //  public Sprite koaHappy, KoaQuestion, KoaSurprised, KoaSerious, KoaConfused, KoaAngry, koaSad, koaAnnoyed, KoaGulty, AzaiSerious, ScottHappy, ScottMad, scottSad, HaniyaHappy, haniyaSad, 
		//NatakamaniHappy, SennacheribMad;

	private void Awake()
	{
        Instance = this;

		//doing this here instead of in inspector so I can search text more easily

		//Koa & co.
		#region Koa & co.
		#region just_landed
		conversationDict["just_landed"] = new();

		ConversationItem just_landed1 = new()
		{
			speakerImage = "KoaSurprised",
			speakerName = "Koa",
			speakerDirection = "Camera",
			speakerText = "Ugh... ow! I just fell out of the sky!"
		};
		conversationDict["just_landed"].Add(just_landed1);

		ConversationItem just_landed2 = new()
		{
			speakerImage = "KoaConfused",
			speakerName = "Koa",
			speakerDirection = "Camera",
			speakerText = "How did I get here? The last thing I remember is taking a nap on the USS Comet... and now I'm here!"
		};
		conversationDict["just_landed"].Add(just_landed2);

		ConversationItem just_landed3 = new()
		{
			speakerImage = "KoaQuestion",
			speakerName = "Koa",
			speakerDirection = "Camera",
			speakerText = "And it's cold! I guess I'll build a fire, maybe someone will see the smoke?"
		};
		conversationDict["just_landed"].Add(just_landed3);

		#endregion
		#region tutorial1
		conversationDict["tutorial1"] = new();

		ConversationItem tutorial11 = new()
		{
			speakerImage = "KoaSerious",
			speakerName = "Koa",
			speakerDirection = "Camera",
			speakerText = "Ok, that will keep me warm for like a few seconds. I could go for some food now..."
		};
		conversationDict["tutorial1"].Add(tutorial11);

		ConversationItem tutorial12 = new()
		{
			speakerImage = "KoaHappy",
			speakerName = "Koa",
			speakerDirection = "Camera",
			speakerText = "(Explore the world by moving the camera around using the arrow or WASD keys. Use Q and E to rotate.)",
			action = true
		};
		conversationDict["tutorial1"].Add(tutorial12);

		ConversationItem tutorial13 = new()
		{
			speakerImage = "KoaHappy",
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
			speakerImage = "KoaHappy",
			speakerName = "Koa",
			speakerDirection = "Camera",
			speakerText = "OK! I can gather some food here."
		};
		conversationDict["tutorial2"].Add(tutorial21);
		#endregion
		#region tutorial3
		conversationDict["tutorial3"] = new();

		ConversationItem tutorial31 = new()
		{
			speakerImage = "KoaQuestion",
			speakerName = "Koa",
			speakerDirection = "Camera",
			speakerText = "Well, that's a little food, I'm going to need some more probably, let's say four more?"
		};
		conversationDict["tutorial3"].Add(tutorial31);
		#endregion
		#region tutorial3a
		conversationDict["tutorial3a"] = new();

		ConversationItem tutorial3a1 = new()
		{
			speakerImage = "KoaConfused",
			speakerName = "Koa",
			speakerDirection = "Camera",
			speakerText = "Who's that guy?"
		};
		conversationDict["tutorial3a"].Add(tutorial3a1);

		ConversationItem tutorial3a2 = new()
		{
			speakerImage = "KoaHappy",
			speakerName = "Koa",
			speakerDirection = "Camera",
			speakerText = "(Walk over to someone to talk to them if they have something to say)"
		};
		conversationDict["tutorial3a"].Add(tutorial3a2);
		#endregion
		#region first_labor
		conversationDict["first_labor"] = new();

		ConversationItem first_labor1 = new()
		{
			speakerImage = "KoaQuestion",
			speakerName = "Koa",
			speakerDirection = "Scott",
			speakerText = "Oh hello... who are you?"
		};
		conversationDict["first_labor"].Add(first_labor1);

		ConversationItem first_labor2 = new()
		{
			speakerImage = "ScottHappy",
			speakerName = "Scott",
			speakerDirection = "Koa",
			speakerText = "Hi! I'm Scott! Isn't it a glorious day today?"
		};
		conversationDict["first_labor"].Add(first_labor2);

		ConversationItem first_labor3 = new()
		{
			speakerImage = "KoaSerious",
			speakerName = "Koa",
			speakerDirection = "Scott",
			speakerText = "Um, yes, it's fine. Where did you come from?"
		};
		conversationDict["first_labor"].Add(first_labor3);

		ConversationItem first_labor4 = new()
		{
			speakerImage = "ScottHappy",
			speakerName = "Scott",
			speakerDirection = "Koa",
			speakerText = "No clue! But I'm glad to be here and happy to help!"
		};
		conversationDict["first_labor"].Add(first_labor4);

		ConversationItem first_labor5 = new()
		{
			speakerImage = "KoaQuestion",
			speakerName = "Koa",
			speakerDirection = "Scott",
			speakerText = "So I'm guessing you showed up when you saw I had some food. Say... could you help me get home?"
		};
		conversationDict["first_labor"].Add(first_labor5);

		ConversationItem first_labor6 = new()
		{
			speakerImage = "ScottHappy",
			speakerName = "Scott",
			speakerDirection = "Koa",
			speakerText = "Sure thing! Do you live right around the corner?"
		};
		conversationDict["first_labor"].Add(first_labor6);

		ConversationItem first_labor7 = new()
		{
			speakerImage = "KoaGulty",
			speakerName = "Koa",
			speakerDirection = "Scott",
			speakerText = "Well, no, it's actually not on this planet... uh, this is kind of a big ask, but could you help me build a rocket?"
		};
		conversationDict["first_labor"].Add(first_labor7);

		ConversationItem first_labor8 = new()
		{
			speakerImage = "ScottHappy",
			speakerName = "Scott",
			speakerDirection = "Koa",
			speakerText = "Absolutely I will! What do I need to do? Also, and this probably isn't important, but what's a rocket? Is it made by rubbing sticks together? I'm great at that!"
		};
		conversationDict["first_labor"].Add(first_labor8);

		ConversationItem first_labor9 = new()
		{
			speakerImage = "KoaQuestion",
			speakerName = "Koa",
			speakerDirection = "Scott",
			speakerText = "Um, I guess it's like a tall, metal cylinder? And it flies into space?"
		};
		conversationDict["first_labor"].Add(first_labor9);

		ConversationItem first_labor10 = new()
		{
			speakerImage = "ScottHappy",
			speakerName = "Scott",
			speakerDirection = "Koa",
			speakerText = "Something that FLIES?! How does it work?"
		};
		conversationDict["first_labor"].Add(first_labor10);

		ConversationItem first_labor11 = new()
		{
			speakerImage = "KoaGulty",
			speakerName = "Koa",
			speakerDirection = "Scott",
			speakerText = "Oh geez, uh, you know, I don't really know how it works... and you clearly don't know much about anything..."
		};
		conversationDict["first_labor"].Add(first_labor11);

		ConversationItem first_labor12 = new()
		{
			speakerImage = "ScottHappy",
			speakerName = "Scott",
			speakerDirection = "Koa",
			speakerText = "*with a big smile* Nope!"
		};
		conversationDict["first_labor"].Add(first_labor12);

		ConversationItem first_labor13 = new()
		{
			speakerImage = "ScottHappy",
			speakerName = "Scott",
			speakerDirection = "Koa",
			speakerText = "We don't have a lot of know-how around here, but if you'd like, we can get more people like me and figure out how to get you what you need!"
		};
		conversationDict["first_labor"].Add(first_labor13);

		ConversationItem first_labor14 = new()
		{
			speakerImage = "KoaHappy",
			speakerName = "Koa",
			speakerDirection = "Scott",
			speakerText = "Really? You will help me? How do we get more people?"
		};
		conversationDict["first_labor"].Add(first_labor14);

		ConversationItem first_labor15 = new()
		{
			speakerImage = "ScottHappy",
			speakerName = "Scott",
			speakerDirection = "Koa",
			speakerText = "Oh it's easy! Just provide food, housing, and water, and so long as you're around, they'll arrive to help!"
		};
		conversationDict["first_labor"].Add(first_labor15);

		ConversationItem first_labor16 = new()
		{
			speakerImage = "ScottHappy",
			speakerName = "Scott",
			speakerDirection = "Koa",
			speakerText = "I can personally help you gather supplies and also build roads! In fact, let's get some lumber to build a hut."
		};
		conversationDict["first_labor"].Add(first_labor16);
		#endregion
		#region tutorial4
		conversationDict["tutorial4"] = new();

		ConversationItem tutorial41 = new()
		{
			speakerImage = "KoaQuestion",
			speakerName = "Koa",
			speakerDirection = "Scott",
			speakerText = "This a good place for lumber?"
		};
		conversationDict["tutorial4"].Add(tutorial41);

		ConversationItem tutorial42 = new()
		{
			speakerImage = "ScottHappy",
			speakerName = "Scott",
			speakerDirection = "Koa",
			speakerText = "Yes, this is a very handsome forest! From here you can get all the lumber you need for housing. Let's together gather 10 lumber."
		};
		conversationDict["tutorial4"].Add(tutorial42);

		ConversationItem tutorial43 = new()
		{
			speakerImage = "KoaConfused",
			speakerName = "Koa",
			speakerDirection = "Scott",
			speakerText = "A \"handsome\" forest?"
		};
		conversationDict["tutorial4"].Add(tutorial43);
		#endregion
		#region tutorial5
		conversationDict["tutorial5"] = new();

		ConversationItem tutorial51 = new()
		{
			speakerImage = "KoaQuestion",
			speakerName = "Koa",
			speakerDirection = "Scott",
			speakerText = "There, that's enough, right? Can we build the hut now in camp?",
			action = true
		};
		conversationDict["tutorial5"].Add(tutorial51);

		ConversationItem tutorial52 = new()
		{
			speakerImage = "KoaHappy",
			speakerName = "Koa",
			speakerDirection = "Camera",
			speakerText = "(Click on the camp to perform actions within the camp.)",
		};
		conversationDict["tutorial5"].Add(tutorial52);
		#endregion
		#region tutorial6
		conversationDict["tutorial6"] = new();

		ConversationItem tutorial61 = new()
		{
			speakerImage = "KoaConfused",
			speakerName = "Koa",
			speakerDirection = "Scott",
			speakerText = "Is that supposed to be someone's house? It's a little small..."
		};
		conversationDict["tutorial6"].Add(tutorial61);

		ConversationItem tutorial62 = new()
		{
			speakerImage = "ScottHappy",
			speakerName = "Scott",
			speakerDirection = "Koa",
			speakerText = "Isn't it quaint? They're going to absolutely love it! Now let's move someone in."
		};
		conversationDict["tutorial6"].Add(tutorial62);

		ConversationItem tutorial63 = new()
		{
			speakerImage = "KoaHappy",
			speakerName = "Koa",
			speakerDirection = "Camera",
			speakerText = "(Click on the camp again)"
		};
		conversationDict["tutorial6"].Add(tutorial63);
		#endregion
		#region tutorial7
		conversationDict["tutorial7"] = new();

		ConversationItem tutorial71 = new()
		{
			speakerImage = "KoaHappy",
			speakerName = "Koa",
			speakerDirection = "Scott",
			speakerText = "Done! Now what?"
		};
		conversationDict["tutorial7"].Add(tutorial71);

		ConversationItem tutorial72 = new()
		{
			speakerImage = "ScottHappy",
			speakerName = "Scott",
			speakerDirection = "Koa",
			speakerText = "Now let's give him something to do! We need to figure out how to build a rocket, right? So let's build a " +
			"place where we can research the necessary technology. To do that we need a lot of stone."
		};
		conversationDict["tutorial7"].Add(tutorial72);
		#endregion
		#region tutorial8
		conversationDict["tutorial8"] = new();

		ConversationItem tutorial81 = new()
		{
			speakerImage = "ScottHappy",
			speakerName = "Scott",
			speakerDirection = "Koa",
			speakerText = "Excellent! Now let's go back to camp and build the research building."
		};
		conversationDict["tutorial8"].Add(tutorial81);
		#endregion
		#region tutorial9
		conversationDict["tutorial9"] = new();

		ConversationItem tutorial91 = new()
		{
			speakerImage = "ScottHappy",
			speakerName = "Scott",
			speakerDirection = "Koa",
			speakerText = "Now that the research building is complete, go to the camp and assign our new pop to work there."
		};
		conversationDict["tutorial9"].Add(tutorial91);
		#endregion
		#region tutorial10
		conversationDict["tutorial10"] = new();

		ConversationItem tutorial101 = new()
		{
			speakerImage = "ScottHappy",
			speakerName = "Scott",
			speakerDirection = "Koa",
			speakerText = "And finally, you just have to choose something to research."
		};
		conversationDict["tutorial10"].Add(tutorial101);
		#endregion
		#region tutorial11
		conversationDict["tutorial11"] = new();

		ConversationItem tutorial111 = new()
		{
			speakerImage = "ScottHappy",
			speakerName = "Scott",
			speakerDirection = "Koa",
			speakerText = "Look! Our new pop is researching now! So long as you have food and money to give him, our people will be happy to research till your heart's content."
		};
		conversationDict["tutorial11"].Add(tutorial111);

		ConversationItem tutorial112 = new()
		{
			speakerImage = "KoaGulty",
			speakerName = "Koa",
			speakerDirection = "Scott",
			speakerText = "Can't they... you know... do it all for free?"
		};
		conversationDict["tutorial11"].Add(tutorial112);

		ConversationItem tutorial113 = new()
		{
			speakerImage = "ScottMad",
			speakerName = "Scott",
			speakerDirection = "Koa",
			speakerText = "No!! A person's labor is not to be stolen, but rather be purchased so that both parties can benefit!"
		};
		conversationDict["tutorial11"].Add(tutorial113);

		ConversationItem tutorial114 = new()
		{
			speakerImage = "KoaSurprised",
			speakerName = "Koa",
			speakerDirection = "Scott",
			speakerText = "Oh, sorry... just wondering..."
		};
		conversationDict["tutorial11"].Add(tutorial114);

		ConversationItem tutorial115 = new()
		{
			speakerImage = "ScottHappy",
			speakerName = "Scott",
			speakerDirection = "Koa",
			speakerText = "*with a big smile again* No problem! In fact, we're currently in need of more currency, so we'll need to gather more resources that our people can purchase."
		};
		conversationDict["tutorial11"].Add(tutorial115);

		ConversationItem tutorial116 = new()
		{
			speakerImage = "ScottHappy",
			speakerName = "Scott",
			speakerDirection = "Koa",
			speakerText = "Let's go gather some more food they can purchase. They won't pay a lot for food since they can gather some themselves, but we'll find real value later with crafted goods."
		};
		conversationDict["tutorial11"].Add(tutorial116);

		ConversationItem tutorial117 = new()
		{
			speakerImage = "KoaHappy",
			speakerName = "Koa",
			speakerDirection = "Camera",
			speakerText = "(Click on any terrain tile to see what resource it holds.)"
		};
		conversationDict["tutorial11"].Add(tutorial117);
		#endregion
		#region tutorial12
		conversationDict["tutorial12"] = new();

		ConversationItem tutorial121 = new()
		{
			speakerImage = "KoaAnnoyed",
			speakerName = "Koa",
			speakerDirection = "Scott",
			speakerText = "Ugh, this is tedious. Are we going to be gathering resources ourselves this whole time?"
		};
		conversationDict["tutorial12"].Add(tutorial121);

		ConversationItem tutorial122 = new()
		{
			speakerImage = "ScottHappy",
			speakerName = "Scott",
			speakerDirection = "Koa",
			speakerText = "Nope! Like I said before, the people here are happy to help. We'll just need to research the proper technologies first before they can do anything."
		};
		conversationDict["tutorial12"].Add(tutorial122);
		#endregion
		#region tutorial13
		conversationDict["tutorial13"] = new();

		ConversationItem tutorial131 = new()
		{
			speakerImage = "ScottHappy",
			speakerName = "Scott",
			speakerDirection = "Koa",
			speakerText = "Our first research is done! Now we can build a farm and hire somebody to work the fields for us. I think you're ready to handle this project, so I won't hold your hand through this one."
		};
		conversationDict["tutorial13"].Add(tutorial131);

		ConversationItem tutorial132 = new()
		{
			speakerImage = "KoaSerious",
			speakerName = "Koa",
			speakerDirection = "Scott",
			speakerText = "Holding MY hand? Uh, I'm doing all the heavy lifting here..."
		};
		conversationDict["tutorial13"].Add(tutorial132);

		ConversationItem tutorial133 = new()
		{
			speakerImage = "ScottHappy",
			speakerName = "Scott",
			speakerDirection = "Koa",
			speakerText = "Oh, you absolutely are! Now to determine what resources you need, you can go to camp and see the costs required to build the farm."
		};
		conversationDict["tutorial13"].Add(tutorial133);
		#endregion
		#region first_infantry
		conversationDict["first_infantry"] = new();

		ConversationItem first_infantry1 = new()
		{
			speakerImage = "KoaHappy",
			speakerName = "Koa",
			speakerDirection = "Azai",
			speakerText = "Hello! Who are you? Another helper for me I hope?"
		};
		conversationDict["first_infantry"].Add(first_infantry1);

		ConversationItem first_infantry2 = new()
		{
			speakerImage = "AzaiSerious",
			speakerName = "Azai",
			speakerDirection = "Koa",
			speakerText = "So you are the one who brought us here... I am Azai, destroyer of nations, harbinger of fear!"
		};
		conversationDict["first_infantry"].Add(first_infantry2);

		ConversationItem first_infantry3 = new()
		{
			speakerImage = "KoaGulty",
			speakerName = "Koa",
			speakerDirection = "Azai",
			speakerText = "Okay, that's a cool name... so you good with grabbing a shovel and..."
		};
		conversationDict["first_infantry"].Add(first_infantry3);

		ConversationItem first_infantry4 = new()
		{
			speakerImage = "ScottHappy",
			speakerName = "Scott",
			speakerDirection = "Azai",
			speakerText = "*interrupting* Hi! I'm Scott! Who are you?"
		};
		conversationDict["first_infantry"].Add(first_infantry4);

		ConversationItem first_infantry5 = new()
		{
			speakerImage = "AzaiSerious",
			speakerName = "Azai",
			speakerDirection = "Scott",
			speakerText = "I am Azai, destroyer of nations, harbinger of fear!"
		};
		conversationDict["first_infantry"].Add(first_infantry5);

		ConversationItem first_infantry6 = new()
		{
			speakerImage = "ScottHappy",
			speakerName = "Scott",
			speakerDirection = "Azai",
			speakerText = "Whoa! Cool name!!"
		};
		conversationDict["first_infantry"].Add(first_infantry6);

		ConversationItem first_infantry7 = new()
		{
			speakerImage = "KoaConfused",
			speakerName = "Koa",
			speakerDirection = "Scott",
			speakerText = "Right, we just said that..."
		};
		conversationDict["first_infantry"].Add(first_infantry7);

		ConversationItem first_infantry8 = new()
		{
			speakerImage = "KoaSerious",
			speakerName = "Koa",
			speakerDirection = "Azai",
			speakerText = "Uh, so, is there a reason you came here? Hoping to add some more adjectives to your name?"
		};
		conversationDict["first_infantry"].Add(first_infantry8);

		ConversationItem first_infantry9 = new()
		{
			speakerImage = "AzaiSerious",
			speakerName = "Azai",
			speakerDirection = "Koa",
			speakerText = "There will be many who will want to take what you have worked for, I have come to develop your armies and ensure your safey traveling these lands."
		};
		conversationDict["first_infantry"].Add(first_infantry9);

		ConversationItem first_infantry10 = new()
		{
			speakerImage = "KoaHappy",
			speakerName = "Koa",
			speakerDirection = "Azai",
			speakerText = "Excellent! Yeah, I think there's a few people out there that don't want us wondering around..."
		};
		conversationDict["first_infantry"].Add(first_infantry10);

		ConversationItem first_infantry11 = new()
		{
			speakerImage = "AzaiSerious",
			speakerName = "Azai",
			speakerDirection = "Koa",
			speakerText = "Verily! Now they will leave us be as we wander this magnificent world, but they hide, guard, and waste what should be used for the progress of humanity. I will train our armies to prevent that."
		};
		conversationDict["first_infantry"].Add(first_infantry11);

		ConversationItem first_infantry12 = new()
		{
			speakerImage = "KoaHappy",
			speakerName = "Koa",
			speakerDirection = "Azai",
			speakerText = "Okay, that's cool man. You can hang out with us I guess, just let me know if you want anything or anything."
		};
		conversationDict["first_infantry"].Add(first_infantry12);
		#endregion
		#region first_pop_loss
		conversationDict["first_pop_loss"] = new();

		ConversationItem first_pop_loss1 = new()
		{
			speakerImage = "ScottSad",
			speakerName = "Scott",
			speakerDirection = "Koa",
			speakerText = "We just lost our first pop... If we don't have enough food, housing or water for a cycle, a pop will leave camp forever. Worst of all, that slightly dampens my otherwise constant ecstatic mood..."
		};
		conversationDict["first_pop_loss"].Add(first_pop_loss1);
		#endregion
		#region first_ambush
		conversationDict["first_ambush"] = new();

		ConversationItem first_ambush1 = new()
		{
			speakerImage = "AzaiSerious",
			speakerName = "Azai",
			speakerDirection = "Koa",
			speakerText = "For the many distant journies our traders make, far away from any city or camp, they risk being ambushed. We must protect them by assigning infantry as guards for traders."
		};
		conversationDict["first_ambush"].Add(first_ambush1);

		ConversationItem first_ambush2 = new()
		{
			speakerImage = "KoaQuestion",
			speakerName = "Koa",
			speakerDirection = "Azai",
			speakerText = "Uh, I don't know if you've noticed, but you're a pretty large dude and you've got a big weapon there, can't you go and guard them?"
		};
		conversationDict["tutorial13"].Add(first_ambush2);

		ConversationItem first_ambush3 = new()
		{
			speakerImage = "AzaiSerious",
			speakerName = "Azai",
			speakerDirection = "Koa",
			speakerText = "My duty, first and foremost, is to ensure your protection from any enemies out there. Without your presence, this will all go to waste. For this reason, I stay by your side."
		};
		conversationDict["first_ambush"].Add(first_ambush3);
		#endregion
		#endregion

		//Haniya
		#region Haniya
		#region intro
		conversationDict["Haniya_intro"] = new();

		ConversationItem haniya_intro = new()
		{
			speakerImage = "HaniyaHappy",
			speakerName = "Haniya",
			speakerDirection = "Koa",
			speakerText = "My name is Haniya"
		};
		conversationDict["Haniya_intro"].Add(haniya_intro);

		#endregion
		#region quest0
		conversationDict["Haniya_quest0"] = new();

		ConversationItem haniya_quest0 = new()
		{
			speakerImage = "HaniyaHappy",
			speakerName = "Haniya",
			speakerDirection = "Koa",
			speakerText = "Could you do this for me"
		};
		conversationDict["Haniya_quest0"].Add(haniya_quest0);

		#endregion
		#endregion

		//Natakamani
		#region Natakamani
		#region intro
		conversationDict["Natakamani_intro"] = new();

		ConversationItem natakamani_intro = new()
		{
			speakerImage = "NatakamaniHappy",
			speakerName = "Natakamani",
			speakerDirection = "Koa",
			speakerText = "My name is Natakamani"
		};
		conversationDict["Natakamani_intro"].Add(natakamani_intro);

		#endregion
		#region quest0
		conversationDict["Natakamani_quest0"] = new();

		ConversationItem natakamani_quest0 = new()
		{
			speakerImage = "NatakamaniHappy",
			speakerName = "Natakamani",
			speakerDirection = "Koa",
			speakerText = "Could you do this for me"
		};
		conversationDict["Natakamani_quest0"].Add(natakamani_quest0);

		#endregion
		#region quest0complete
		conversationDict["Natakamani_quest0_complete"] = new();

		ConversationItem natakamani_quest0_complete = new()
		{
			speakerImage = "NatakamaniHappy",
			speakerName = "Natakamani",
			speakerDirection = "Koa",
			speakerText = "Thank you"
		};
		conversationDict["Natakamani_quest0_complete"].Add(natakamani_quest0_complete);

		#endregion
		#region quest1
		conversationDict["Natakamani_quest1"] = new();

		ConversationItem natakamani_quest1 = new()
		{
			speakerImage = "NatakamaniHappy",
			speakerName = "Natakamani",
			speakerDirection = "Koa",
			speakerText = "Could you do this for me this time"
		};
		conversationDict["Natakamani_quest1"].Add(natakamani_quest1);

		#endregion
		#region quest1complete
		conversationDict["Natakamani_quest1_complete"] = new();

		ConversationItem natakamani_quest1_complete = new()
		{
			speakerImage = "NatakamaniHappy",
			speakerName = "Natakamani",
			speakerDirection = "Koa",
			speakerText = "Thank you again"
		};
		conversationDict["Natakamani_quest1_complete"].Add(natakamani_quest1_complete);

		#endregion
		#endregion

		//Sennacherib
		#region Sennacherib
		#region intro
		conversationDict["Sennacherib_intro"] = new();

		ConversationItem sennacherib_intro = new()
		{
			speakerImage = "SennacheribMad",
			speakerName = "Sennacherib",
			speakerDirection = "Koa",
			speakerText = "My name is Sennacherib"
		};
		conversationDict["Sennacherib_intro"].Add(sennacherib_intro);
		#endregion
		#region challenge0
		conversationDict["Sennacherib_challenge0"] = new();

		ConversationItem sennacherib_challenge0 = new()
		{
			speakerImage = "SennacheribMad",
			speakerName = "Sennacherib",
			speakerDirection = "Koa",
			speakerText = "I challenge you to a fight"
		};
		conversationDict["Sennacherib_challenge0"].Add(sennacherib_challenge0);
		#endregion
		#endregion
	}
}

[Serializable]
public struct ConversationItem
{
    public string speakerImage;
    public string speakerName;
	public string speakerDirection;
    public string speakerText;
	public bool action;
	public bool condition;
}
