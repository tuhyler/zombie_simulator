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

		#region Koa & Co.
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
			speakerText = "How did I get here? The last thing I remember is taking a nap on my couch and having a really weird dream about flying through space... and now I'm here!"
		};
		conversationDict["just_landed"].Add(just_landed2);

		ConversationItem just_landed3 = new()
		{
			speakerImage = "KoaQuestion",
			speakerName = "Koa",
			speakerDirection = "Camera",
			speakerText = "Where am I? Am I even on Earth anymore? I need to set up camp until I figure out what's going on."
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
			speakerText = "Ok, that will keep me warm for a few seconds. I need to go find some food now..."
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
			speakerText = "Okay! I can gather some food here."
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
			speakerText = "Oh hello... who are you?",
			action = true
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
			speakerText = "I'm from here! I actually used to live in a cave in that mountain over there with my wife, till a bear came along and -- Ooh, pretty fire!"
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
			speakerImage = "KoaGuilty",
			speakerName = "Koa",
			speakerDirection = "Scott",
			speakerText = "Well, no, it's actually not on this planet... I think in order for me to get home, I'm going to have to build a rocket."
		};
		conversationDict["first_labor"].Add(first_labor7);

		ConversationItem first_labor8 = new()
		{
			speakerImage = "ScottHappy",
			speakerName = "Scott",
			speakerDirection = "Koa",
			speakerText = "I can help with that! What do I need to do? Also, and this probably isn't important, but what's a rocket? Is it made by rubbing sticks together? I'm great at that!"
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
			speakerImage = "KoaGuilty",
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
			speakerText = "I don't have a lot of know-how, but if you have any tasks that you say are possible to complete, then I'll help make it happen!"
		};
		conversationDict["first_labor"].Add(first_labor13);

		ConversationItem first_labor14 = new()
		{
			speakerImage = "KoaHappy",
			speakerName = "Koa",
			speakerDirection = "Scott",
			speakerText = "Really? You will help me?"
		};
		conversationDict["first_labor"].Add(first_labor14);

		ConversationItem first_labor15 = new()
		{
			speakerImage = "KoaThinking",
			speakerName = "Koa",
			speakerDirection = "Scott",
			speakerText = "Well, let me think... I need to make a rocket, the peak of all human accomplishments, but you and I don't know how to make anything, " +
			"which means we have to figure it all out ourselves..."
		};
		conversationDict["first_labor"].Add(first_labor15);

		ConversationItem first_labor16 = new()
		{
			speakerImage = "KoaGuilty",
			speakerName = "Koa",
			speakerDirection = "Scott",
			speakerText = "So, uh, this is kind of a big ask... but could you help me recreate all of human history?"
		};
		conversationDict["first_labor"].Add(first_labor16);

		ConversationItem first_labor17 = new()
		{
			speakerImage = "ScottHappy",
			speakerName = "Scott",
			speakerDirection = "Koa",
			speakerText = "Absolutely I will! That doesn't sound like it will take very long! Just let me know what I can do!"
		};
		conversationDict["first_labor"].Add(first_labor17);
		#endregion
		#region tutorial3b
		conversationDict["tutorial3b"] = new();

		ConversationItem tutorial3b1 = new()
		{
			speakerImage = "ScottHappy",
			speakerName = "Scott",
			speakerDirection = "Koa",
			speakerText = "If you'd like, this task could be completed even faster if we get more people."
		};
		conversationDict["tutorial3b"].Add(tutorial3b1);

		ConversationItem tutorial3b2 = new()
		{
			speakerImage = "KoaQuestion",
			speakerName = "Koa",
			speakerDirection = "Scott",
			speakerText = "We can do that? How do we get more people?"
		};
		conversationDict["tutorial3b"].Add(tutorial3b2);

		ConversationItem tutorial3b3 = new()
		{
			speakerImage = "ScottHappy",
			speakerName = "Scott",
			speakerDirection = "Koa",
			speakerText = "It's easy! We can get more people by simply gathering food, building shelter, and providing water, and they will come."
		};
		conversationDict["tutorial3b"].Add(tutorial3b3);

		ConversationItem tutorial3b4 = new()
		{
			speakerImage = "ScottHappy",
			speakerName = "Scott",
			speakerDirection = "Koa",
			speakerText = "We already have some food and water, so let's start by getting some lumber to build a hut!"
		};
		conversationDict["tutorial3b"].Add(tutorial3b4);
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
			speakerText = "Yes, this is a very handsome forest! From here you can get all the lumber you need for housing. Let's together gather 6 lumber."
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
			"place where we can research the necessary technology. To do that, we need to get a lot of stone."
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
			speakerText = "Look! Our new pop is researching now! So long as you have food and money to provide, our people will be happy to research till your heart's content."
		};
		conversationDict["tutorial11"].Add(tutorial111);

		ConversationItem tutorial112 = new()
		{
			speakerImage = "KoaGuilty",
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
			speakerText = "No!! A person's labor is not to be stolen, but rather be purchased for the benefit of all parties involved!"
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
			speakerText = "*smiling again* No problem! In fact, we're currently in need of more currency, so we'll need to gather more resources that our people can purchase. " +
			"Each camp makes all their purchases at the end of each cycle."
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
		#region last_resource
		conversationDict["last_resource"] = new();

		ConversationItem last_resource1 = new()
		{
			speakerImage = "KoaAnnoyed",
			speakerName = "Koa",
			speakerDirection = "Scott",
			speakerText = "Ugh, this is tedious. Are we going to be gathering resources ourselves this whole time?"
		};
		conversationDict["last_resource"].Add(last_resource1);

		ConversationItem last_resource2 = new()
		{
			speakerImage = "ScottHappy",
			speakerName = "Scott",
			speakerDirection = "Koa",
			speakerText = "Nope! Like I said before, the people here are happy to help. We'll just need to research the proper technologies first before they can do anything."
		};
		conversationDict["last_resource"].Add(last_resource2);
		#endregion
		#region agriculture
		conversationDict["agriculture"] = new();

		ConversationItem agriculture1 = new()
		{
			speakerImage = "ScottHappy",
			speakerName = "Scott",
			speakerDirection = "Koa",
			speakerText = "Our first research is done! Now we can build a farm and hire somebody to work the fields for us. I think you're ready to handle this project, so I won't hold your hand through this one."
		};
		conversationDict["agriculture"].Add(agriculture1);

		ConversationItem agriculture2 = new()
		{
			speakerImage = "KoaSerious",
			speakerName = "Koa",
			speakerDirection = "Scott",
			speakerText = "Holding MY hand? Uh, I'm doing all the heavy lifting here..."
		};
		conversationDict["agriculture"].Add(agriculture2);

		ConversationItem agriculture3 = new()
		{
			speakerImage = "ScottHappy",
			speakerName = "Scott",
			speakerDirection = "Koa",
			speakerText = "Oh, you absolutely are! Now to determine what resources you need, you can go to camp and see the costs required to build the farm."
		};
		conversationDict["agriculture"].Add(agriculture3);
		#endregion
		#region pottery
		conversationDict["pottery"] = new();

		ConversationItem pottery1 = new()
		{
			speakerImage = "ScottHappy",
			speakerName = "Scott",
			speakerDirection = "Koa",
			speakerText = "We can now build a ceramist! This is a producer that will create a crafted good for which our people will be willing to spend much more money. " +
			"The more of these we make, the more money we can pay to our researchers!"
		};
		conversationDict["pottery"].Add(pottery1);

		ConversationItem pottery2 = new()
		{
			speakerImage = "KoaHappy",
			speakerName = "Koa",
			speakerDirection = "Camera",
			speakerText = "(Go to a nearby city and camp and click on the \"Sea Market Pricds\" button to learn more about selling prices and quantities.)"
		};
		conversationDict["pottery"].Add(pottery2);
		#endregion
		#region trade
		conversationDict["trade"] = new();

		ConversationItem trade1 = new()
		{
			speakerImage = "ScottHappy",
			speakerName = "Scott",
			speakerDirection = "Koa",
			speakerText = "We can now build a trade depot! Trade depots allow us to build traders and they are a great way to move resources from one city to another."
		};
		conversationDict["trade"].Add(trade1);
		#endregion
		#region first_farm
		conversationDict["first_farm"] = new();

		ConversationItem first_farm1 = new()
		{
			speakerImage = "ScottHappy",
			speakerName = "Scott",
			speakerDirection = "Koa",
			speakerText = "Great! Our first farm is finished! Good job finding out what resources we needed to make it."
		};
		conversationDict["first_farm"].Add(first_farm1);

		ConversationItem first_farm2 = new()
		{
			speakerImage = "ScottHappy",
			speakerName = "Scott",
			speakerDirection = "Koa",
			speakerText = "Now we need to assign somebody to work there. Remember, to add more pop, we need to build more huts (housing). After that's done, go back into the camp to add more pop."
		};
		conversationDict["first_farm"].Add(first_farm2);
		#endregion
		#region first_infantry
		conversationDict["first_infantry"] = new();

		ConversationItem first_infantry1 = new()
		{
			speakerImage = "KoaHappy",
			speakerName = "Koa",
			speakerDirection = "Azai",
			speakerText = "Hello! Who are you? Another helper for me I hope?",
			action = true
		};
		conversationDict["first_infantry"].Add(first_infantry1);

		ConversationItem first_infantry2 = new()
		{
			speakerImage = "AzaiSerious",
			speakerName = "Azai",
			speakerDirection = "Koa",
			speakerText = "I am Azai, conqueror of Ximara and former ruler of Tavaria!"
		};
		conversationDict["first_infantry"].Add(first_infantry2);

		ConversationItem first_infantry3 = new()
		{
			speakerImage = "KoaGuilty",
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
			speakerText = "*Interrupting* Wow! You were the ruler of Tavaria? What are you doing here?"
		};
		conversationDict["first_infantry"].Add(first_infantry4);

		ConversationItem first_infantry5 = new()
		{
			speakerImage = "AzaiSerious",
			speakerName = "Azai",
			speakerDirection = "Scott",
			speakerText = "I have taken leave of my position as a noble ruler to encounter new challenges in distant lands. " +
			"I stopped here because I noticed that you and your people are clearly undertaking a massive task. Is that true? "
		};
		conversationDict["first_infantry"].Add(first_infantry5);

		ConversationItem first_infantry6 = new()
		{
			speakerImage = "KoaGuilty",
			speakerName = "Koa",
			speakerDirection = "Azai",
			speakerText = "You have no idea.... "
		};
		conversationDict["first_infantry"].Add(first_infantry6);

		ConversationItem first_infantry7 = new()
		{
			speakerImage = "KoaQuestion",
			speakerName = "Koa",
			speakerDirection = "Azai",
			speakerText = "So that's why you're here? To watch? Or were you hoping to add some more adjectives to your name?"
		};
		conversationDict["first_infantry"].Add(first_infantry7);

		ConversationItem first_infantry8 = new()
		{
			speakerImage = "AzaiSerious",
			speakerName = "Azai",
			speakerDirection = "Koa",
			speakerText = "Many will come to attempt to destroy what you have worked for. Indeed, the more you labor, the more they will labor in return to tear it all down. " +
			"Therefore, I will develop your armies to ensure your safety in these lands."
		};
		conversationDict["first_infantry"].Add(first_infantry8);

		ConversationItem first_infantry9 = new()
		{
			speakerImage = "KoaHappy",
			speakerName = "Koa",
			speakerDirection = "Azai",
			speakerText = "Excellent! Yeah, I think there's a few people out there that don't want us wondering around..."
		};
		conversationDict["first_infantry"].Add(first_infantry9);

		ConversationItem first_infantry10 = new()
		{
			speakerImage = "AzaiSerious",
			speakerName = "Azai",
			speakerDirection = "Koa",
			speakerText = "Verily! Considering the scale of your project, my armies will also help to acquire new resources, " +
			"jealousy guarded by those that would only waste what has been given them."
		};
		conversationDict["first_infantry"].Add(first_infantry10);

		ConversationItem first_infantry11 = new()
		{
			speakerImage = "KoaHappy",
			speakerName = "Koa",
			speakerDirection = "Azai",
			speakerText = "Cool, thanks for the help! You know, you speak like someone who is perpetually practicing for a Shakespeare play."
		};
		conversationDict["first_infantry"].Add(first_infantry11);
		#endregion
		#region hunting
		conversationDict["hunting"] = new();

		ConversationItem hunting1 = new()
		{
			speakerImage = "AzaiSerious",
			speakerName = "Azai",
			speakerDirection = "Koa",
			speakerText = "Now that I have joined forces with you, we can build a barracks in any city. " +
			"The barracks will serve to build military units, which can be used to protect the aforementioned city, or be sent out to acquire new lands."
		};
		conversationDict["hunting"].Add(hunting1);

		ConversationItem hunting2 = new()
		{
			speakerImage = "AzaiSerious",
			speakerName = "Azai",
			speakerDirection = "Koa",
			speakerText = "When you have acquired enough resources in your city, build a barracks so that we may begin training our infantry."
		};
		conversationDict["hunting"].Add(hunting2);
		#endregion
		#region first_built_infantry
		conversationDict["first_built_infantry"] = new();

		ConversationItem first_built_infantry1 = new()
		{
			speakerImage = "AzaiSerious",
			speakerName = "Azai",
			speakerDirection = "Koa",
			speakerText = "We have just built our first infantry unit! Select him to deploy him to attack an enemy camp. Keep in mind that support must be provided to send out an army."
		};
		conversationDict["first_built_infantry"].Add(first_built_infantry1);

		ConversationItem first_built_infantry2 = new()
		{
			speakerImage = "KoaQuestion",
			speakerName = "Koa",
			speakerDirection = "Azai",
			speakerText = "I can't help but notice he looks exactly like you. Hmm... what's a guy to think..."
		};
		conversationDict["first_built_infantry"].Add(first_built_infantry2);

		ConversationItem first_built_infantry3 = new()
		{
			speakerImage = "AzaiSerious",
			speakerName = "Azai",
			speakerDirection = "Koa",
			speakerText = "Just because he's also devastatingly handsome, it doesn't mean he looks like me. You need to start noticing differentiating details in one's appearance."
		};
		conversationDict["first_built_infantry"].Add(first_built_infantry3);
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
		conversationDict["first_ambush"].Add(first_ambush2);

		ConversationItem first_ambush3 = new()
		{
			speakerImage = "AzaiSerious",
			speakerName = "Azai",
			speakerDirection = "Koa",
			speakerText = "My duty, first and foremost, is to ensure your protection from any enemies out there. Without your presence, this will all go to waste. For this reason, I stay by your side."
		};
		conversationDict["first_ambush"].Add(first_ambush3);
		#endregion
		#region first_trader
		conversationDict["first_trader"] = new();

		ConversationItem first_trader1 = new()
		{
			speakerImage = "ScottHappy",
			speakerName = "Scott",
			speakerDirection = "Koa",
			speakerText = "We just built our first trader! Now we can start moving resources from one city to another " +
			"or even start automatically buying resources from a nearby trade center. Select a trader to assign him a route."
		};
		conversationDict["first_trader"].Add(first_trader1);
		#endregion
		#region new_city
		conversationDict["new_city"] = new();

		ConversationItem new_city1 = new()
		{
			speakerImage = "ScottHappy",
			speakerName = "Scott",
			speakerDirection = "Koa",
			speakerText = "One of our camps just becamse a city! If a camp reaches the pop amount of four or more, then it becomes a city with a new name displayed overhead. " +
			"Traders will then recognize that city under its new name for their stops on their route."
		};
		conversationDict["new_city"].Add(new_city1);
		#endregion
		#region azaidefeat
		conversationDict["azaidefeat"] = new();

		ConversationItem azaidefeat1 = new()
		{
			speakerImage = "AzaiSerious",
			speakerName = "Azai",
			speakerDirection = "Koa",
			speakerText = "He is stronger than what I had previously anticipated."
		};
		conversationDict["azaidefeat"].Add(azaidefeat1);

		ConversationItem azaidefeat2 = new()
		{
			speakerImage = "KoaQuestion",
			speakerName = "Koa",
			speakerDirection = "Azai",
			speakerText = "Do you think you can defeat him? He's just going to keep sending guys at us until you do."
		};
		conversationDict["azaidefeat"].Add(azaidefeat2);

		ConversationItem azaidefeat3 = new()
		{
			speakerImage = "AzaiSerious",
			speakerName = "Azai",
			speakerDirection = "Koa",
			speakerText = "Yes, but I need to be better prepared. I need to fully recover my health and will most certainly need a better weapon. " +
			"Let us return to a nearby city and upgrade the current weapon and armor I have, which I assume will also make me much more devastatingly handsome."
		};
		conversationDict["azaidefeat"].Add(azaidefeat3);
		#endregion
		#region new_finance_center
		conversationDict["new_finance_center"] = new();

		ConversationItem new_finance_center1= new()
		{
			speakerImage = "ScottHappy",
			speakerName = "Scott",
			speakerDirection = "Koa",
			speakerText = "We just built our first finance center! Adding labor to these centers will increase the amount of goods purchased each cycle for every " +
			"sellable item (except for food). These centers are a great way to increase our sales!"
		};
		conversationDict["new_finance_center"].Add(new_finance_center1);
		#endregion
		#region new_entertainment_center
		conversationDict["new_entertainment_center"] = new();

		ConversationItem new_entertainment_center1 = new()
		{
			speakerImage = "ScottHappy",
			speakerName = "Scott",
			speakerDirection = "Koa",
			speakerText = "We just built our first entertainment center! Adding labor to these centers will make the pop of our cities very happy, " +
			"thus increasing their work ethic and the amount of goods they produce!"
		};
		conversationDict["new_entertainment_center"].Add(new_entertainment_center1);
		#endregion
		#region new_producer
		conversationDict["new_producer"] = new();

		ConversationItem new_producer1 = new()
		{
			speakerImage = "ScottHappy",
			speakerName = "Scott",
			speakerDirection = "Koa",
			speakerText = "We just built our first goods producer! It's difficult to run an economy just on food sales alone, so selling higher priced goods created by producers, " +
			"such as pottery or cloth, can generate much higher sales for us!"
		};
		conversationDict["new_producer"].Add(new_producer1);
		#endregion
		#region next_city
		conversationDict["next_city"] = new();

		ConversationItem next_city1 = new()
		{
			speakerImage = "ScottHappy",
			speakerName = "Scott",
			speakerDirection = "Koa",
			speakerText = "We just built another city! I can manually move resources to this new city to help develop it more quickly, and later, we can build traders to move the resources for us."
		};
		conversationDict["next_city"].Add(next_city1);

		ConversationItem next_city2 = new()
		{
			speakerImage = "KoaHappy",
			speakerName = "Koa",
			speakerDirection = "Camera",
			speakerText = "(While standing on a city center, click the Load button to move resources. Ctrl-click to move an entire resource pile, shift-click to move half a resource pile.)"
		};
		conversationDict["next_city"].Add(next_city2);
		#endregion
		#endregion

		#region Haniya
		#region intro
		conversationDict["Haniya_intro"] = new();

		ConversationItem haniya_intro0 = new()
		{
			speakerImage = "HaniyaHappy",
			speakerName = "Haniya",
			speakerDirection = "Koa",
			speakerText = "Hi! I'm Haniya, and I run the trade operations here out of Indus Valley, can I interest you in some spices?"
		};
		conversationDict["Haniya_intro"].Add(haniya_intro0);

		ConversationItem haniya_intro1 = new()
		{
			speakerImage = "KoaHappy",
			speakerName = "Koa",
			speakerDirection = "Haniya",
			speakerText = "Hello, I'm Koa. It's nice to--"
		};
		conversationDict["Haniya_intro"].Add(haniya_intro1);

		ConversationItem haniya_intro2 = new()
		{
			speakerImage = "AzaiSerious",
			speakerName = "Azai",
			speakerDirection = "Haniya",
			speakerText = "*Interrupting* Greetings! I am Azai, conqueror of Ximara and ruler of Tavaria!"
		};
		conversationDict["Haniya_intro"].Add(haniya_intro2);

		ConversationItem haniya_intro3 = new()
		{
			speakerImage = "HaniyaHappy",
			speakerName = "Haniya",
			speakerDirection = "Azai",
			speakerText = "Hi Azai! I like your name, sounds like you've had a lot adventures!"
		};
		conversationDict["Haniya_intro"].Add(haniya_intro3);

		ConversationItem haniya_intro4 = new()
		{
			speakerImage = "KoaQuestion",
			speakerName = "Koa",
			speakerDirection = "Haniya",
			speakerText = "Yeah, he's had an exhausting life. Anyways, do you sell anything that you think our people would buy?"
		};
		conversationDict["Haniya_intro"].Add(haniya_intro4);

		ConversationItem haniya_intro5 = new()
		{
			speakerImage = "HaniyaHappy",
			speakerName = "Haniya",
			speakerDirection = "Koa",
			speakerText = "I do! Like I said before, I have some of the finest spices on the continent, plus some--"
		};
		conversationDict["Haniya_intro"].Add(haniya_intro5);

		ConversationItem haniya_intro6 = new()
		{
			speakerImage = "AzaiSerious",
			speakerName = "Azai",
			speakerDirection = "Haniya",
			speakerText = "*Loudly* Perhaps the lady would like to hear me recount my tales of strength and valor, such as when I bested the fierce Ximaran warrior, Hemar!"
		};
		conversationDict["Haniya_intro"].Add(haniya_intro6);

		ConversationItem haniya_intro7 = new()
		{
			speakerImage = "HaniyaAwkward",
			speakerName = "Haniya",
			speakerDirection = "Azai",
			speakerText = "Um, maybe another time? I'm just very excited to show all of you my wares that I have to offer."
		};
		conversationDict["Haniya_intro"].Add(haniya_intro7);

		ConversationItem haniya_intro8 = new()
		{
			speakerImage = "KoaAnnoyed",
			speakerName = "Koa",
			speakerDirection = "Azai",
			speakerText = "Yes, Azai, let's save the stories of besting others and how much we can bench and whatever for later."
		};
		conversationDict["Haniya_intro"].Add(haniya_intro8);

		ConversationItem haniya_intro9 = new()
		{
			speakerImage = "HaniyaHappy",
			speakerName = "Haniya",
			speakerDirection = "Koa",
			speakerText = "Well, I'm glad you stopped by Indus Valley! We have plenty of goods to sell so feel free to stop by at any time. " +
			"I'll also be glad to purchase any merchandise you offer as well."
		};
		conversationDict["Haniya_intro"].Add(haniya_intro9);

		ConversationItem haniya_intro10 = new()
		{
			speakerImage = "ScottHappy",
			speakerName = "Scott",
			speakerDirection = "Haniya",
			speakerText = "Sounds good! We'll be back soon!",
			action = true
		};
		conversationDict["Haniya_intro"].Add(haniya_intro10);
		#endregion
		#region intro_coda
		conversationDict["Haniya_intro_coda"] = new();

		ConversationItem haniya_intro_coda0 = new()
		{
			speakerImage = "AzaiSerious",
			speakerName = "Azai",
			speakerDirection = "Koa",
			speakerText = "*Clears throat loudly*"
		};
		conversationDict["Haniya_intro_coda"].Add(haniya_intro_coda0);

		ConversationItem haniya_intro_coda1 = new()
		{
			speakerImage = "KoaQuestion",
			speakerName = "Koa",
			speakerDirection = "Azai",
			speakerText = "Yes? What is it?"
		};
		conversationDict["Haniya_intro_coda"].Add(haniya_intro_coda1);

		ConversationItem haniya_intro_coda2 = new()
		{
			speakerImage = "AzaiAwkward",
			speakerName = "Azai",
			speakerDirection = "Koa",
			speakerText = "So do you think she liked me?"
		};
		conversationDict["Haniya_intro_coda"].Add(haniya_intro_coda2);

		ConversationItem haniya_intro_coda3 = new()
		{
			speakerImage = "KoaConfused",
			speakerName = "Koa",
			speakerDirection = "Azai",
			speakerText = "What?"
		};
		conversationDict["Haniya_intro_coda"].Add(haniya_intro_coda3);

		ConversationItem haniya_intro_coda4 = new()
		{
			speakerImage = "AzaiSerious",
			speakerName = "Azai",
			speakerDirection = "Koa",
			speakerText = "I am simply querying if the lady fancies someone, someone... such as myself, perhaps."
		};
		conversationDict["Haniya_intro_coda"].Add(haniya_intro_coda4);

		ConversationItem haniya_intro_coda5 = new()
		{
			speakerImage = "KoaQuestion",
			speakerName = "Koa",
			speakerDirection = "Azai",
			speakerText = "Who, Haniya? Um, well, you may have come on a bit strong with your loud declarations of your past accomplishments..."
		};
		conversationDict["Haniya_intro_coda"].Add(haniya_intro_coda5);

		ConversationItem haniya_intro_coda6 = new()
		{
			speakerImage = "AzaiAwkward",
			speakerName = "Azai",
			speakerDirection = "Koa",
			speakerText = "Maybe you can suggest what I should do then? I know I'm not much to look at and I've never been able to express myself " +
			"very well, so perhaps you can offer guidance..."
		};
		conversationDict["Haniya_intro_coda"].Add(haniya_intro_coda6);

		ConversationItem haniya_intro_coda7 = new()
		{
			speakerImage = "KoaQuestion",
			speakerName = "Koa",
			speakerDirection = "Azai",
			speakerText = "Well, maybe next time, don't focus so much on yourself? I'd say try to learn more about her by asking her lots of questions."
		};
		conversationDict["Haniya_intro_coda"].Add(haniya_intro_coda7);

		ConversationItem haniya_intro_coda8 = new()
		{
			speakerImage = "AzaiSerious",
			speakerName = "Azai",
			speakerDirection = "Koa",
			speakerText = "*Loudly* Your challenge is accepted, sir! When we return to visit Haniya, I shall know everything there is to know about our newest acquaintance, " +
			"from the number of years she's lived to the amount she weighs!"
		};
		conversationDict["Haniya_intro_coda"].Add(haniya_intro_coda8);

		ConversationItem haniya_intro_coda9 = new()
		{
			speakerImage = "KoaQuestion",
			speakerName = "Koa",
			speakerDirection = "Azai",
			speakerText = "*Not really listening* Yeah, you got it, that's the spirit!"
		};
		conversationDict["Haniya_intro_coda"].Add(haniya_intro_coda9);
		#endregion
		#region quest0
		conversationDict["Haniya_quest0"] = new();

		ConversationItem haniya_quest01 = new()
		{
			speakerImage = "HaniyaHappy",
			speakerName = "Haniya",
			speakerDirection = "Koa",
			speakerText = "Hi guys! I'm so glad you came back!"
		};
		conversationDict["Haniya_quest0"].Add(haniya_quest01);

		ConversationItem haniya_quest02 = new()
		{
			speakerImage = "AzaiAwkward",
			speakerName = "Azai",
			speakerDirection = "Haniya",
			speakerText = "*Clearly nervous* Greetings, Haniya! Uh, how are you on this fine day? How was your morning? Um, what did you have for breakfast? " +
			"If you could be any animal, what would it be and why? "
		};
		conversationDict["Haniya_quest0"].Add(haniya_quest02);

		ConversationItem haniya_quest03 = new()
		{
			speakerImage = "HaniyaSad",
			speakerName = "Haniya",
			speakerDirection = "Koa",
			speakerText = "Oh, I'm not doing very well at all, actually. The roof of my house has a terrible leak and I need some containers to catch the leaking water. " +
			"Do you have anything that you could offer to help me with my problem?"
		};
		conversationDict["Haniya_quest0"].Add(haniya_quest03);

		ConversationItem haniya_quest04 = new()
		{
			speakerImage = "HaniyaQuestion",
			speakerName = "Haniya",
			speakerDirection = "Koa",
			speakerText = "I would need exactly 5 of these items. I will, of course, compensate you for your time and offering."
		};
		conversationDict["Haniya_quest0"].Add(haniya_quest04);

		ConversationItem haniya_quest05 = new()
		{
			speakerImage = "KoaQuestion",
			speakerName = "Koa",
			speakerDirection = "Haniya",
			speakerText = "Yeah, I think we can get something like that, what do you say, Azai?"
		};
		conversationDict["Haniya_quest0"].Add(haniya_quest05);

		ConversationItem haniya_quest06 = new()
		{
			speakerImage = "AzaiSerious",
			speakerName = "Azai",
			speakerDirection = "Haniya",
			speakerText = "Haniya, will our completion of your request make you happy?"
		};
		conversationDict["Haniya_quest0"].Add(haniya_quest06);

		ConversationItem haniya_quest07 = new()
		{
			speakerImage = "HaniyaHappy",
			speakerName = "Haniya",
			speakerDirection = "Azai",
			speakerText = "*With a big smile* Yes! Very much so!"
		};
		conversationDict["Haniya_quest0"].Add(haniya_quest07);

		ConversationItem haniya_quest08 = new()
		{
			speakerImage = "AzaiSerious",
			speakerName = "Azai",
			speakerDirection = "Haniya",
			speakerText = "Then we shall waste no time in retrieving what you need! Onward, my fellow contemporaries!"
		};
		conversationDict["Haniya_quest0"].Add(haniya_quest08);
		#endregion
		#region quest0complete
		conversationDict["Haniya_quest0_complete"] = new();

		ConversationItem haniya_quest0_complete1 = new()
		{
			speakerImage = "HaniyaHappy",
			speakerName = "Haniya",
			speakerDirection = "Koa",
			speakerText = "Thank you so much for your help! I was worried I was going to have to turn my house into an indoor pool and start running a day spa."
		};
		conversationDict["Haniya_quest0_complete"].Add(haniya_quest0_complete1);

		ConversationItem haniya_quest0_complete2 = new()
		{
			speakerImage = "KoaHappy",
			speakerName = "Koa",
			speakerDirection = "Haniya",
			speakerText = "That's not a bad idea, actually. You could call it \"Sparadise\" and overcharge for lemon water."
		};
		conversationDict["Haniya_quest0_complete"].Add(haniya_quest0_complete2);

		ConversationItem haniya_quest0_complete3 = new()
		{
			speakerImage = "HaniyaHappy",
			speakerName = "Haniya",
			speakerDirection = "Koa",
			speakerText = "Thanks for the suggestion, but I think I'd rather just have my house back. Oh, and Azai?"
		};
		conversationDict["Haniya_quest0_complete"].Add(haniya_quest0_complete3);

		ConversationItem haniya_quest0_complete4 = new()
		{
			speakerImage = "AzaiSerious",
			speakerName = "Azai",
			speakerDirection = "Haniya",
			speakerText = "Yes, Haniya?"
		};
		conversationDict["Haniya_quest0_complete"].Add(haniya_quest0_complete4);

		ConversationItem haniya_quest0_complete5 = new()
		{
			speakerImage = "HaniyaHappy",
			speakerName = "Haniya",
			speakerDirection = "Azai",
			speakerText = "If I could be any animal, I would be a dog, because I love being around others and I'm fiercely loyal to " +
			"those I love the most. What about you?"
		};
		conversationDict["Haniya_quest0_complete"].Add(haniya_quest0_complete5);

		ConversationItem haniya_quest0_complete6 = new()
		{
			speakerImage = "AzaiEmbarrassed",
			speakerName = "Azai",
			speakerDirection = "Haniya",
			speakerText = "I would be a stag, the adult male deer, because stags are protective leaders of their herd, " +
			"gracefully standing up to danger whenever necessary."
		};
		conversationDict["Haniya_quest0_complete"].Add(haniya_quest0_complete6);

		ConversationItem haniya_quest0_complete7 = new()
		{
			speakerImage = "KoaQuestion",
			speakerName = "Koa",
			speakerDirection = "Azai",
			speakerText = "That's a pretty pathetic animal to pick from someone who's always talking about \"conquering\" and \"ruling\"."
		};
		conversationDict["Haniya_quest0_complete"].Add(haniya_quest0_complete7);

		ConversationItem haniya_quest0_complete8 = new()
		{
			speakerImage = "HaniyaHappy",
			speakerName = "Haniya",
			speakerDirection = "Azai",
			speakerText = "I thought it was a great animal to choose! I love deer!"
		};
		conversationDict["Haniya_quest0_complete"].Add(haniya_quest0_complete8);

		ConversationItem haniya_quest0_complete9 = new()
		{
			speakerImage = "AzaiHappy",
			speakerName = "Azai",
			speakerDirection = "Haniya",
			speakerText = "*Smiles*",
			action = true
		};
		conversationDict["Haniya_quest0_complete"].Add(haniya_quest0_complete9);
		#endregion
		#region quest0complete_coda
		conversationDict["Haniya_quest0_complete_coda"] = new();

		ConversationItem haniya_quest0_complete_coda1 = new()
		{
			speakerImage = "AzaiSerious",
			speakerName = "Azai",
			speakerDirection = "Koa",
			speakerText = "*Ahem* That went quite well, I would say. Our interaction with Haniya, I mean."
		};
		conversationDict["Haniya_quest0_complete_coda"].Add(haniya_quest0_complete_coda1);

		ConversationItem haniya_quest0_complete_coda2 = new()
		{
			speakerImage = "ScottHappy",
			speakerName = "Scott",
			speakerDirection = "Azai",
			speakerText = "Yes, I thought it went great! She really responded well to what you had to say. Great job, Azai!"
		};
		conversationDict["Haniya_quest0_complete_coda"].Add(haniya_quest0_complete_coda2);

		ConversationItem haniya_quest0_complete_coda3 = new()
		{
			speakerImage = "AzaiSerious",
			speakerName = "Azai",
			speakerDirection = "Koa",
			speakerText = "Koa, seeing as your previous advice was so successful, any other tokens of wisdom you'd wish to part with?"
		};
		conversationDict["Haniya_quest0_complete_coda"].Add(haniya_quest0_complete_coda3);

		ConversationItem haniya_quest0_complete_coda4 = new()
		{
			speakerImage = "KoaQuestion",
			speakerName = "Koa",
			speakerDirection = "Azai",
			speakerText = "Well, in my experience with the lady folk, they seem to prefer a man with a high level of confidence."
		};
		conversationDict["Haniya_quest0_complete_coda"].Add(haniya_quest0_complete_coda4);

		ConversationItem haniya_quest0_complete_coda5 = new()
		{
			speakerImage = "AzaiSerious",
			speakerName = "Azai",
			speakerDirection = "Koa",
			speakerText = "So NOW I can divulge my feats of strength? Of defeating the mighty warrior, Hemar? " +
			"In truth, I was wearing a full suit of armor for the battle, but it would be more impressive if I were shirtless, so I'll recount it that way."
		};
		conversationDict["Haniya_quest0_complete_coda"].Add(haniya_quest0_complete_coda5);

		ConversationItem haniya_quest0_complete_coda6 = new()
		{
			speakerImage = "KoaQuestion",
			speakerName = "Koa",
			speakerDirection = "Azai",
			speakerText = "Settle down, Hercules, I don't think you want to come off as arrogant, or overly intimidating. " +
			"Just, you know, act like it's not a big deal if she doesn't go for you, like you just don't care or whatever."
		};
		conversationDict["Haniya_quest0_complete_coda"].Add(haniya_quest0_complete_coda6);

		ConversationItem haniya_quest0_complete_coda7 = new()
		{
			speakerImage = "AzaiSerious",
			speakerName = "Azai",
			speakerDirection = "Koa",
			speakerText = "Like I don't care... acknowledged!"
		};
		conversationDict["Haniya_quest0_complete_coda"].Add(haniya_quest0_complete_coda7);
		#endregion
		#region quest1
		conversationDict["Haniya_quest1"] = new();

		ConversationItem haniya_quest11 = new()
		{
			speakerImage = "HaniyaHappy",
			speakerName = "Haniya",
			speakerDirection = "Koa",
			speakerText = "Hello everyone! And thanks again for your help earlier, those vases have been very helpful in catching the water from the leaking roof."
		};
		conversationDict["Haniya_quest1"].Add(haniya_quest11);

		ConversationItem haniya_quest12 = new()
		{
			speakerImage = "KoaHappy",
			speakerName = "Koa",
			speakerDirection = "Haniya",
			speakerText = "Glad to hear it, anything else we can get for you, preferably for some additional coins?"
		};
		conversationDict["Haniya_quest1"].Add(haniya_quest12);

		ConversationItem haniya_quest13 = new()
		{
			speakerImage = "HaniyaQuestion",
			speakerName = "Haniya",
			speakerDirection = "Koa",
			speakerText = "Actually, yes! I'm gathering all the supplies to fix the roof, but I need some more material, do you have 10 bricks you can part with?"
		};
		conversationDict["Haniya_quest1"].Add(haniya_quest13);

		ConversationItem haniya_quest14 = new()
		{
			speakerImage = "ScottHappy",
			speakerName = "Scott",
			speakerDirection = "Haniya",
			speakerText = "Absolutely we can provide that for you, right everyone? We'll head home and round those items up right away!"
		};
		conversationDict["Haniya_quest1"].Add(haniya_quest14);

		ConversationItem haniya_quest15 = new()
		{
			speakerImage = "HaniyaSad",
			speakerName = "Haniya",
			speakerDirection = "Koa",
			speakerText = "Thanks again for your help! I've been so worried about that leaking roof overhead, what if the leaks get worse and it floods my house? " +
			"Or what if the roof collapses while I'm sleeping and it flattens me?"
		};
		conversationDict["Haniya_quest1"].Add(haniya_quest15);

		ConversationItem haniya_quest16 = new()
		{
			speakerImage = "AzaiSerious",
			speakerName = "Azai",
			speakerDirection = "Haniya",
			speakerText = "Well, just so you know, I couldn't care less about this entire situation."
		};
		conversationDict["Haniya_quest1"].Add(haniya_quest16);

		ConversationItem haniya_quest17 = new()
		{
			speakerImage = "HaniyaSad",
			speakerName = "Haniya",
			speakerDirection = "Azai",
			speakerText = "What!?"
		};
		conversationDict["Haniya_quest1"].Add(haniya_quest17);

		ConversationItem haniya_quest18 = new()
		{
			speakerImage = "AzaiSerious",
			speakerName = "Azai",
			speakerDirection = "Haniya",
			speakerText = "Like I said, I can't think of anything in this entire world that I could possibly care less about. Hu-WAH! *Yells and thumps chest*"
		};
		conversationDict["Haniya_quest1"].Add(haniya_quest18);

		ConversationItem haniya_quest19 = new()
		{
			speakerImage = "HaniyaSad",
			speakerName = "Haniya",
			speakerDirection = "Azai",
			speakerText = "Oh... well, if that's how you feel, you don't have to--"
		};
		conversationDict["Haniya_quest1"].Add(haniya_quest19);

		ConversationItem haniya_quest110 = new()
		{
			speakerImage = "KoaSurprised",
			speakerName = "Koa",
			speakerDirection = "Haniya",
			speakerText = "We can help! We'll get you your stuff! Sorry about Azai, he's just grumpy because he hasn't " +
			"killed something in a while..."
		};
		conversationDict["Haniya_quest1"].Add(haniya_quest110);
		#endregion
		#region quest1complete
		conversationDict["Haniya_quest1_complete"] = new();

		ConversationItem haniya_quest1_complete1 = new()
		{
			speakerImage = "HaniyaHappy",
			speakerName = "Haniya",
			speakerDirection = "Koa",
			speakerText = "Thanks for retrieving these bricks! This will definitely help me sleep better at night. And here's a little something for your trouble, as promised."
		};
		conversationDict["Haniya_quest1_complete"].Add(haniya_quest1_complete1);

		ConversationItem haniya_quest1_complete2 = new()
		{
			speakerImage = "AzaiSerious",
			speakerName = "Azai",
			speakerDirection = "Haniya",
			speakerText = "For the record, I really don't care--"
		};
		conversationDict["Haniya_quest1_complete"].Add(haniya_quest1_complete2);

		ConversationItem haniya_quest1_complete3 = new()
		{
			speakerImage = "KoaAngry",
			speakerName = "Koa",
			speakerDirection = "Azai",
			speakerText = "Dude!"
		};
		conversationDict["Haniya_quest1_complete"].Add(haniya_quest1_complete3);

		ConversationItem haniya_quest1_complete4 = new()
		{
			speakerImage = "KoaQuestion",
			speakerName = "Koa",
			speakerDirection = "Haniya",
			speakerText = "Glad we could help, Haniya, and thanks for your business. We'll be seeing you later, after Azai and I have a little discussion...",
			action = true
		};
		conversationDict["Haniya_quest1_complete"].Add(haniya_quest1_complete4);
		#endregion
		#region quest1complete_coda
		conversationDict["Haniya_quest1_complete_coda"] = new();

		ConversationItem haniya_quest1_complete_coda1 = new()
		{
			speakerImage = "AzaiSerious",
			speakerName = "Azai",
			speakerDirection = "Koa",
			speakerText = "Your previous advice to help me with Haniya did not seem to be particularly effective."
		};
		conversationDict["Haniya_quest1_complete_coda"].Add(haniya_quest1_complete_coda1);

		ConversationItem haniya_quest1_complete_coda2 = new()
		{
			speakerImage = "KoaAngry",
			speakerName = "Koa",
			speakerDirection = "Azai",
			speakerText = "That's because you took it way too literally! Maybe the next time around you should stay quiet, you can hope Haniya likes the strong and silent type."
		};
		conversationDict["Haniya_quest1_complete_coda"].Add(haniya_quest1_complete_coda2);

		ConversationItem haniya_quest1_complete_coda3 = new()
		{
			speakerImage = "ScottSerious",
			speakerName = "Scott",
			speakerDirection = "Azai",
			speakerText = "Azai, as your closest and dearest friend..."
		};
		conversationDict["Haniya_quest1_complete_coda"].Add(haniya_quest1_complete_coda3);

		ConversationItem haniya_quest1_complete_coda4 = new()
		{
			speakerImage = "KoaQuestion",
			speakerName = "Koa",
			speakerDirection = "Scott",
			speakerText = "You guys just barely met like an hour ago."
		};
		conversationDict["Haniya_quest1_complete_coda"].Add(haniya_quest1_complete_coda4);

		ConversationItem haniya_quest1_complete_coda5 = new()
		{
			speakerImage = "ScottSerious",
			speakerName = "Scott",
			speakerDirection = "Azai",
			speakerText = "...might I suggest apologizing to her? After twenty years of marriage, I've found that apologizing has " +
			"always worked for me whenever I feel like I've said the wrong thing to my wife."
		};
		conversationDict["Haniya_quest1_complete_coda"].Add(haniya_quest1_complete_coda5);

		ConversationItem haniya_quest1_complete_coda6 = new()
		{
			speakerImage = "AzaiSerious",
			speakerName = "Azai",
			speakerDirection = "Scott",
			speakerText = "Nonsense! Apologies are for foppling lubberworts!"
		};
		conversationDict["Haniya_quest1_complete_coda"].Add(haniya_quest1_complete_coda6);

		ConversationItem haniya_quest1_complete_coda7 = new()
		{
			speakerImage = "KoaQuestion",
			speakerName = "Koa",
			speakerDirection = "Azai",
			speakerText = "Apologies are for what? You sound like you just crawled out of a Charles Dickenes novel."
		};
		conversationDict["Haniya_quest1_complete_coda"].Add(haniya_quest1_complete_coda7);

		ConversationItem haniya_quest1_complete_coda8 = new()
		{
			speakerImage = "ScottSerious",
			speakerName = "Scott",
			speakerDirection = "Azai",
			speakerText = "Still, if you really like her, maybe give it a try?"
		};
		conversationDict["Haniya_quest1_complete_coda"].Add(haniya_quest1_complete_coda8);

		ConversationItem haniya_quest1_complete_coda9 = new()
		{
			speakerImage = "AzaiSerious",
			speakerName = "Azai",
			speakerDirection = "Scott",
			speakerText = "Hrrmmph!"
		};
		conversationDict["Haniya_quest1_complete_coda"].Add(haniya_quest1_complete_coda9);
		#endregion
		#region quest2
		conversationDict["Haniya_quest2"] = new();

		ConversationItem haniya_quest21 = new()
		{
			speakerImage = "HaniyaHappy",
			speakerName = "Haniya",
			speakerDirection = "Koa",
			speakerText = "Hello again! What have you been up to since we last talked?"
		};
		conversationDict["Haniya_quest2"].Add(haniya_quest21);

		ConversationItem haniya_quest22 = new()
		{
			speakerImage = "KoaHappy",
			speakerName = "Koa",
			speakerDirection = "Haniya",
			speakerText = "Oh, you know, a little of this, a little of that. Any chance we can get you something for some coins?"
		};
		conversationDict["Haniya_quest2"].Add(haniya_quest22);

		ConversationItem haniya_quest23 = new()
		{
			speakerImage = "HaniyaQuestion",
			speakerName = "Haniya",
			speakerDirection = "Koa",
			speakerText = "Well, I'm in the process of fixing my roof, but I need cloth, specifically 8, to help keep things clean. Is that something you could help with?"
		};
		conversationDict["Haniya_quest2"].Add(haniya_quest23);

		ConversationItem haniya_quest24 = new()
		{
			speakerImage = "ScottHappy",
			speakerName = "Scott",
			speakerDirection = "Haniya",
			speakerText = "Delighted to help! We'll get on that straight away!"
		};
		conversationDict["Haniya_quest2"].Add(haniya_quest24);

		ConversationItem haniya_quest25 = new()
		{
			speakerImage = "HaniyaHappy",
			speakerName = "Haniya",
			speakerDirection = "Scott",
			speakerText = "Sounds great! You all are absolute life savers."
		};
		conversationDict["Haniya_quest2"].Add(haniya_quest25);

		ConversationItem haniya_quest26 = new()
		{
			speakerImage = "ScottSerious",
			speakerName = "Scott",
			speakerDirection = "Haniya",
			speakerText = "But Haniya, before we go, I believe Azai has something he'd like to say."
		};
		conversationDict["Haniya_quest2"].Add(haniya_quest26);

		ConversationItem haniya_quest27 = new()
		{
			speakerImage = "HaniyaQuestion",
			speakerName = "Haniya",
			speakerDirection = "Azai",
			speakerText = "*Cautiously* Yes, Azai? What is it?"
		};
		conversationDict["Haniya_quest2"].Add(haniya_quest27);

		ConversationItem haniya_quest28 = new()
		{
			speakerImage = "AzaiAwkward",
			speakerName = "Azai",
			speakerDirection = "Haniya",
			speakerText = "*Mumbling incoherently* mamjerfpwerhubarbgrerwalmrhubarbyalm..."
		};
		conversationDict["Haniya_quest2"].Add(haniya_quest28);

		ConversationItem haniya_quest29 = new()
		{
			speakerImage = "HaniyaQuestion",
			speakerName = "Haniya",
			speakerDirection = "Azai",
			speakerText = "Sorry, what was that?"
		};
		conversationDict["Haniya_quest2"].Add(haniya_quest29);

		ConversationItem haniya_quest210 = new()
		{
			speakerImage = "AzaiAwkward",
			speakerName = "Azai",
			speakerDirection = "Haniya",
			speakerText = "*Staring wide-eyed at the horizon*"
		};
		conversationDict["Haniya_quest2"].Add(haniya_quest210);

		ConversationItem haniya_quest211 = new()
		{
			speakerImage = "HaniyaQuestion",
			speakerName = "Haniya",
			speakerDirection = "Koa",
			speakerText = "Is he going to say something?"
		};
		conversationDict["Haniya_quest2"].Add(haniya_quest211);

		ConversationItem haniya_quest212 = new()
		{
			speakerImage = "KoaQuestion",
			speakerName = "Koa",
			speakerDirection = "Haniya",
			speakerText = "I'm not sure, I think he's determining if he's a \"foppling lubberwort\" or not."
		};
		conversationDict["Haniya_quest2"].Add(haniya_quest212);
		#endregion
		#region quest2complete
		conversationDict["Haniya_quest2_complete"] = new();

		ConversationItem haniya_quest2_complete1 = new()
		{
			speakerImage = "HaniyaHappy",
			speakerName = "Haniya",
			speakerDirection = "Koa",
			speakerText = "Wow! Thanks for getting that so quickly! And here's a little something for you!"
		};
		conversationDict["Haniya_quest2_complete"].Add(haniya_quest2_complete1);

		ConversationItem haniya_quest2_complete2 = new()
		{
			speakerImage = "KoaHappy",
			speakerName = "Koa",
			speakerDirection = "Haniya",
			speakerText = "Much appreciated! This last one was barely out of our way to go get."
		};
		conversationDict["Haniya_quest2_complete"].Add(haniya_quest2_complete2);

		ConversationItem haniya_quest2_complete3 = new()
		{
			speakerImage = "ScottHappy",
			speakerName = "Scott",
			speakerDirection = "Haniya",
			speakerText = "Yes indeedy! And you be sure to let us know if there's anything else!"
		};
		conversationDict["Haniya_quest2_complete"].Add(haniya_quest2_complete3);

		ConversationItem haniya_quest2_complete4 = new()
		{
			speakerImage = "HaniyaHappy",
			speakerName = "Haniya",
			speakerDirection = "Scott",
			speakerText = "Oh, definitely! I've learned I can count on you all to deliver. I'll talk to you later!"
		};
		conversationDict["Haniya_quest2_complete"].Add(haniya_quest2_complete4);

		ConversationItem haniya_quest2_complete5 = new()
		{
			speakerImage = "AzaiSerious",
			speakerName = "Azai",
			speakerDirection = "Haniya",
			speakerText = "Wait... Haniya?"
		};
		conversationDict["Haniya_quest2_complete"].Add(haniya_quest2_complete5);

		ConversationItem haniya_quest2_complete6 = new()
		{
			speakerImage = "HaniyaQuestion",
			speakerName = "Haniya",
			speakerDirection = "Azai",
			speakerText = "Yes?"
		};
		conversationDict["Haniya_quest2_complete"].Add(haniya_quest2_complete6);

		ConversationItem haniya_quest2_complete7 = new()
		{
			speakerImage = "AzaiAwkward",
			speakerName = "Azai",
			speakerDirection = "Haniya",
			speakerText = "*Looking down at the ground, drawing in the dirt with his toe* I just wanted to say.. you know... about that thing earlier..."
		};
		conversationDict["Haniya_quest2_complete"].Add(haniya_quest2_complete7);

		ConversationItem haniya_quest2_complete8 = new()
		{
			speakerImage = "HaniyaQuestion",
			speakerName = "Haniya",
			speakerDirection = "Azai",
			speakerText = "Go on..."
		};
		conversationDict["Haniya_quest2_complete"].Add(haniya_quest2_complete8);

		ConversationItem haniya_quest2_complete9 = new()
		{
			speakerImage = "AzaiSerious",
			speakerName = "Azai",
			speakerDirection = "Haniya",
			speakerText = "I'm sorry for my previous comments. I wasn't quite myself earlier, and I was stupidly following advice from this idiot that I know."
		};
		conversationDict["Haniya_quest2_complete"].Add(haniya_quest2_complete9);

		ConversationItem haniya_quest2_complete10 = new()
		{
			speakerImage = "KoaSurprised",
			speakerName = "Koa",
			speakerDirection = "Azai",
			speakerText = "Hey!"
		};
		conversationDict["Haniya_quest2_complete"].Add(haniya_quest2_complete10);

		ConversationItem haniya_quest2_complete11 = new()
		{
			speakerImage = "HaniyaHappy",
			speakerName = "Haniya",
			speakerDirection = "Azai",
			speakerText = "Oh, that's okay, Azai. What you said was so out of character that I assumed you weren't quite yourself."
		};
		conversationDict["Haniya_quest2_complete"].Add(haniya_quest2_complete11);

		ConversationItem haniya_quest2_complete12 = new()
		{
			speakerImage = "AzaiHappy",
			speakerName = "Azai",
			speakerDirection = "Haniya",
			speakerText = "Oh, I am overcome with relief! Not to mention how pleased I am to have completed this \"apology\" task!",
			action = true
		};
		conversationDict["Haniya_quest2_complete"].Add(haniya_quest2_complete12);
		#endregion
		#region quest2complete_coda
		conversationDict["Haniya_quest2_complete_coda"] = new();

		ConversationItem haniya_quest2_complete_coda1 = new()
		{
			speakerImage = "AzaiSerious",
			speakerName = "Azai",
			speakerDirection = "Koa",
			speakerText = "We must be sure to return to Haniya."
		};
		conversationDict["Haniya_quest2_complete_coda"].Add(haniya_quest2_complete_coda1);

		ConversationItem haniya_quest2_complete_coda2 = new()
		{
			speakerImage = "KoaHappy",
			speakerName = "Koa",
			speakerDirection = "Azai",
			speakerText = "Oh yeah? *In a teasing voice* Do you MISS her? Do you want to be with her ALL THE TIME?"
		};
		conversationDict["Haniya_quest2_complete_coda"].Add(haniya_quest2_complete_coda2);

		ConversationItem haniya_quest2_complete_coda3 = new()
		{
			speakerImage = "AzaiSerious",
			speakerName = "Azai",
			speakerDirection = "Koa",
			speakerText = "Yes, of course. I have clearly been interested ever since we met her, and I even told you as much from the very beginning."
		};
		conversationDict["Haniya_quest2_complete_coda"].Add(haniya_quest2_complete_coda3);

		ConversationItem haniya_quest2_complete_coda4 = new()
		{
			speakerImage = "KoaGuilty",
			speakerName = "Koa",
			speakerDirection = "Azai",
			speakerText = "Oh. Right."
		};
		conversationDict["Haniya_quest2_complete_coda"].Add(haniya_quest2_complete_coda4);

		ConversationItem haniya_quest2_complete_coda5 = new()
		{
			speakerImage = "ScottHappy",
			speakerName = "Scott",
			speakerDirection = "Azai",
			speakerText = "Great idea, Azai! Yes, we should go back to see her as soon as we can! I'd like to hear about her progress on that roof of hers."
		};
		conversationDict["Haniya_quest2_complete_coda"].Add(haniya_quest2_complete_coda5);
		#endregion
		#region quest3
		conversationDict["Haniya_quest3"] = new();

		ConversationItem haniya_quest31 = new()
		{
			speakerImage = "KoaHappy",
			speakerName = "Koa",
			speakerDirection = "Haniya",
			speakerText = "Hi Haniya, we're back! How's it going? How's the roof of your house?"
		};
		conversationDict["Haniya_quest3"].Add(haniya_quest31);

		ConversationItem haniya_quest32 = new()
		{
			speakerImage = "HaniyaHappy",
			speakerName = "Haniya",
			speakerDirection = "Koa",
			speakerText = "Welcome back, Koa! Good news! I fixed the roof! I don't have to worry about it caving in on me anymore!"
		};
		conversationDict["Haniya_quest3"].Add(haniya_quest32);

		ConversationItem haniya_quest33 = new()
		{
			speakerImage = "AzaiHappy",
			speakerName = "Azai",
			speakerDirection = "Haniya",
			speakerText = "Excellent work, Haniya! You handled this issue with a confidence and intelligence seen only from the best of Tavaria!"
		};
		conversationDict["Haniya_quest3"].Add(haniya_quest33);

		ConversationItem haniya_quest34 = new()
		{
			speakerImage = "HaniyaHappy",
			speakerName = "Haniya",
			speakerDirection = "Azai",
			speakerText = "*Beaming* Thanks, Azai! I appreciate it."
		};
		conversationDict["Haniya_quest3"].Add(haniya_quest34);

		ConversationItem haniya_quest35 = new()
		{
			speakerImage = "HaniyaQuestion",
			speakerName = "Haniya",
			speakerDirection = "Koa",
			speakerText = "Actually, I could use your help again. With all the repairs done on the house, it's gotten pretty stinky. " +
			"Any chance you could get me something to make it smell better? I wouldn't need too much, let's say 5?"
		};
		conversationDict["Haniya_quest3"].Add(haniya_quest35);

		ConversationItem haniya_quest36 = new()
		{
			speakerImage = "KoaQuestion",
			speakerName = "Koa",
			speakerDirection = "Haniya",
			speakerText = "After spending time with these two, I can say that you are asking the absolute worst people about tips on smelling good."
		};
		conversationDict["Haniya_quest3"].Add(haniya_quest36);

		ConversationItem haniya_quest37 = new()
		{
			speakerImage = "KoaHappy",
			speakerName = "Koa",
			speakerDirection = "Haniya",
			speakerText = "However, I might be able to scrounge something up for you!"
		};
		conversationDict["Haniya_quest3"].Add(haniya_quest37);

		ConversationItem haniya_quest38 = new()
		{
			speakerImage = "HaniyaHappy",
			speakerName = "Haniya",
			speakerDirection = "Koa",
			speakerText = "Yay! Great, thanks!"
		};
		conversationDict["Haniya_quest3"].Add(haniya_quest38);
		#endregion
		#region quest3complete
		conversationDict["Haniya_quest3_complete"] = new();

		ConversationItem haniya_quest3_complete1 = new()
		{
			speakerImage = "HaniyaHappy",
			speakerName = "Haniya",
			speakerDirection = "Koa",
			speakerText = "This is perfect! Thank you so much! My house is going to smell so much better now!"
		};
		conversationDict["Haniya_quest3_complete"].Add(haniya_quest3_complete1);

		ConversationItem haniya_quest3_complete2 = new()
		{
			speakerImage = "KoaQuestion",
			speakerName = "Koa",
			speakerDirection = "Haniya",
			speakerText = "Yeah, glad to get that for you. I was hoping Scott or Azai could get some, or just roll around in it or something, " +
			"but they didn't pick up on any of my hints, unfortunately. "
		};
		conversationDict["Haniya_quest3_complete"].Add(haniya_quest3_complete2);

		ConversationItem haniya_quest3_complete3 = new()
		{
			speakerImage = "AzaiSerious",
			speakerName = "Azai",
			speakerDirection = "Haniya",
			speakerText = "*Throwing his chest out* A man's musk is nothing to be ashamed of and should be displayed as a sign of his strength and hard work."
		};
		conversationDict["Haniya_quest3_complete"].Add(haniya_quest3_complete3);

		ConversationItem haniya_quest3_complete4 = new()
		{
			speakerImage = "AzaiAwkward",
			speakerName = "Azai",
			speakerDirection = "Haniya",
			speakerText = "Unless you don't like the smell, Haniya? I don't want to make you uncomfortable or anything..."
		};
		conversationDict["Haniya_quest3_complete"].Add(haniya_quest3_complete4);

		ConversationItem haniya_quest3_complete5 = new()
		{
			speakerImage = "HaniyaHappy",
			speakerName = "Haniya",
			speakerDirection = "Azai",
			speakerText = "*Smiling* I like the way you smell, Azai! Of course, I'm used to the smell of sewage because of my house right now, so my judgment may be a little off. "
		};
		conversationDict["Haniya_quest3_complete"].Add(haniya_quest3_complete5);

		ConversationItem haniya_quest3_complete6 = new()
		{
			speakerImage = "HaniyaHappy",
			speakerName = "Haniya",
			speakerDirection = "Koa",
			speakerText = "Oh, and before I forget, here's your payment for your help. I do hope you came back soon!"
		};
		conversationDict["Haniya_quest3_complete"].Add(haniya_quest3_complete6);
		#endregion
		#region quest3complete_coda
		conversationDict["Haniya_quest3_complete_coda"] = new();

		ConversationItem haniya_quest3_complete_coda1 = new()
		{
			speakerImage = "AzaiSerious",
			speakerName = "Azai",
			speakerDirection = "Koa",
			speakerText = "I would be a fool to not ask out Haniya the next chance I get."
		};
		conversationDict["Haniya_quest3_complete_coda"].Add(haniya_quest3_complete_coda1);

		ConversationItem haniya_quest3_complete_coda2 = new()
		{
			speakerImage = "KaiQuestion",
			speakerName = "Koa",
			speakerDirection = "Azai",
			speakerText = "Yeah, it's about time you got on that. People around here are starting to say that you're too scared to do it..."
		};
		conversationDict["Haniya_quest3_complete_coda"].Add(haniya_quest3_complete_coda2);

		ConversationItem haniya_quest3_complete_coda3 = new()
		{
			speakerImage = "AzaiSerious",
			speakerName = "Azai",
			speakerDirection = "Koa",
			speakerText = "You are lying, of course, because nobody with even a half of a brain would even consider saying something like that about me, but I understand your point. "
		};
		conversationDict["Haniya_quest3_complete_coda"].Add(haniya_quest3_complete_coda3);

		ConversationItem haniya_quest3_complete_coda4 = new()
		{
			speakerImage = "ScottHappy",
			speakerName = "Scott",
			speakerDirection = "Azai",
			speakerText = "I think that's a fantastic idea, Azai! When are you going to do it? The next time we return here?"
		};
		conversationDict["Haniya_quest3_complete_coda"].Add(haniya_quest3_complete_coda4);

		ConversationItem haniya_quest3_complete_coda5 = new()
		{
			speakerImage = "AzaiAwkward",
			speakerName = "Azai",
			speakerDirection = "Scott",
			speakerText = "*Timidly* The next time? Isn't that a little too soon? Don't you think it would be better if we waited a few more times, or until we make it through the next winter?"
		};
		conversationDict["Haniya_quest3_complete_coda"].Add(haniya_quest3_complete_coda5);

		ConversationItem haniya_quest3_complete_coda6 = new()
		{
			speakerImage = "KaiQuestion",
			speakerName = "Koa",
			speakerDirection = "Azai",
			speakerText = "Are you hoping she dies of old age first so you don't have to do it?"
		};
		conversationDict["Haniya_quest3_complete_coda"].Add(haniya_quest3_complete_coda6);

		ConversationItem haniya_quest3_complete_coda7 = new()
		{
			speakerImage = "AzaiAwkward",
			speakerName = "Azai",
			speakerDirection = "Scott",
			speakerText = "*Glares* I just want to take advantage of the most appropriate time, but perhaps sooner is better than later..."
		};
		conversationDict["Haniya_quest3_complete_coda"].Add(haniya_quest3_complete_coda7);
		#endregion
		#region quest4
		conversationDict["Haniya_quest4"] = new();

		ConversationItem haniya_quest41 = new()
		{
			speakerImage = "HaniyaHappy",
			speakerName = "Haniya",
			speakerDirection = "Koa",
			speakerText = "Hello again, Koa! I was hopeful you'd return soon, I have another favor to ask of you all. Would you be able to provide me bronze ingots that you can use to form weapons? " +
			"I would need 9 of them."
		};
		conversationDict["Haniya_quest4"].Add(haniya_quest41);

		ConversationItem haniya_quest42 = new()
		{
			speakerImage = "KoaHappy",
			speakerName = "Koa",
			speakerDirection = "Haniya",
			speakerText = "Yeah we can do that, but before we leave, I think Azai wanted to ask you something, right Azai?"
		};
		conversationDict["Haniya_quest4"].Add(haniya_quest42);

		ConversationItem haniya_quest43 = new()
		{
			speakerImage = "HaniyaHappy",
			speakerName = "Haniya",
			speakerDirection = "Azai",
			speakerText = "Oh? What is it, Azai?"
		};
		conversationDict["Haniya_quest4"].Add(haniya_quest43);

		ConversationItem haniya_quest44 = new()
		{
			speakerImage = "AzaiAwkward",
			speakerName = "Azai",
			speakerDirection = "Haniya",
			speakerText = "Um, yes, er, well, for what purpose did you intend to use these ingots?"
		};
		conversationDict["Haniya_quest4"].Add(haniya_quest44);

		ConversationItem haniya_quest45 = new()
		{
			speakerImage = "HaniyaHappy",
			speakerName = "Haniya",
			speakerDirection = "Azai",
			speakerText = "Oh, it's not for me, it's something my boyfriend wanted and I wanted to give them to him as a surprise!"
		};
		conversationDict["Haniya_quest4"].Add(haniya_quest45);

		ConversationItem haniya_quest46 = new()
		{
			speakerImage = "AzaiSerious",
			speakerName = "Azai",
			speakerDirection = "Haniya",
			speakerText = "...for your... boyfriend?"
		};
		conversationDict["Haniya_quest4"].Add(haniya_quest46);

		ConversationItem haniya_quest47 = new()
		{
			speakerImage = "HaniyaHappy",
			speakerName = "Haniya",
			speakerDirection = "Azai",
			speakerText = "Yup! We just started seeing each other, it's kind of a funny story how we met, turns out he was the one who accidentally broke my roof in the first place! " +
			"He was saving a kitten from an overhanging branch, but the branch broke, and he fell on my roof! (The kitten was okay.)"
		};
		conversationDict["Haniya_quest4"].Add(haniya_quest47);

		ConversationItem haniya_quest48 = new()
		{
			speakerImage = "HaniyaHappy",
			speakerName = "Haniya",
			speakerDirection = "Azai",
			speakerText = "Was there anything else you wanted? Azai?"
		};
		conversationDict["Haniya_quest4"].Add(haniya_quest48);

		ConversationItem haniya_quest49 = new()
		{
			speakerImage = "KoaGuilty",
			speakerName = "Koa",
			speakerDirection = "Haniya",
			speakerText = "Er no... probably not anymore..."
		};
		conversationDict["Haniya_quest4"].Add(haniya_quest49);

		ConversationItem haniya_quest410 = new()
		{
			speakerImage = "HaniyaHappy",
			speakerName = "Haniya",
			speakerDirection = "Azai",
			speakerText = "No? Okay! Well, thanks for stopping by, everyone!"
		};
		conversationDict["Haniya_quest4"].Add(haniya_quest410);
		#endregion
		#region quest4complete
		conversationDict["Haniya_quest4_complete"] = new();

		ConversationItem haniya_quest4_complete1 = new()
		{
			speakerImage = "HaniyaHappy",
			speakerName = "Haniya",
			speakerDirection = "Koa",
			speakerText = "Thank you again! These are very hard to come by, so I'm glad you were able to deliver."
		};
		conversationDict["Haniya_quest4_complete"].Add(haniya_quest4_complete1);

		ConversationItem haniya_quest4_complete2 = new()
		{
			speakerImage = "AzaiSerious",
			speakerName = "Azai",
			speakerDirection = "Haniya",
			speakerText = "This bronze will make an excellent gift to the man in your life, who is no doubt a very lucky individual."
		};
		conversationDict["Haniya_quest4_complete"].Add(haniya_quest4_complete2);

		ConversationItem haniya_quest4_complete3 = new()
		{
			speakerImage = "KoaQuestion",
			speakerName = "Koa",
			speakerDirection = "Azai",
			speakerText = "Wow Azai, that was a very mature response."
		};
		conversationDict["Haniya_quest4_complete"].Add(haniya_quest4_complete3);

		ConversationItem haniya_quest4_complete4 = new()
		{
			speakerImage = "AzaiSerious",
			speakerName = "Azai",
			speakerDirection = "Haniya",
			speakerText = "*Flexing* However, I must now return to our blossoming civilization, for I am needed to single-handedly fend off all enemy invaders, " +
			"save HUNDREDS of kittens stuck in trees, talk about my deepest feelings and desires, and so on."
		};
		conversationDict["Haniya_quest4_complete"].Add(haniya_quest4_complete4);

		ConversationItem haniya_quest4_complete5 = new()
		{
			speakerImage = "KoaQuestion",
			speakerName = "Koa",
			speakerDirection = "Azai",
			speakerText = "Ah, there's the Azai I know."
		};
		conversationDict["Haniya_quest4_complete"].Add(haniya_quest4_complete5);

		ConversationItem haniya_quest4_complete6 = new()
		{
			speakerImage = "HaniyaHappy",
			speakerName = "Haniya",
			speakerDirection = "Koa",
			speakerText = "Wow! Sounds like you're really busy! Well, goodbye everyone! Thanks again for your help!"
		};
		conversationDict["Haniya_quest4_complete"].Add(haniya_quest4_complete6);
		#endregion
		#endregion

		#region Natakamani
		#region intro
		conversationDict["Natakamani_intro"] = new();

		ConversationItem natakamani_intro1 = new()
		{
			speakerImage = "NatakamaniHappy",
			speakerName = "Natakamani",
			speakerDirection = "Koa",
			speakerText = "Greetings travelers! I am Natakamani, king of the Kush!"
		};
		conversationDict["Natakamani_intro"].Add(natakamani_intro1);

		ConversationItem natakamani_intro2 = new()
		{
			speakerImage = "KoaHappy",
			speakerName = "Koa",
			speakerDirection = "Natakamani",
			speakerText = "Hi, Natakamani! And I am Koa, king of this land I just fell on, apparently."
		};
		conversationDict["Natakamani_intro"].Add(natakamani_intro2);

		ConversationItem natakamani_intro3 = new()
		{
			speakerImage = "NatakamaniHappy",
			speakerName = "Natakamani",
			speakerDirection = "Koa",
			speakerText = "Welcome to this great city of Meroe, the very city where I was raised by my mother, Amanitore. Here we provide the finest goods that you and " +
			"your people will surely find to be extremely appealing."
		};
		conversationDict["Natakamani_intro"].Add(natakamani_intro3);

		ConversationItem natakamani_intro4 = new()
		{
			speakerImage = "KoaHappy",
			speakerName = "Koa",
			speakerDirection = "Natakamani",
			speakerText = "This is a cool city, Natakamani. I like those mini-pyramids you have over there."
		};
		conversationDict["Natakamani_intro"].Add(natakamani_intro4);

		ConversationItem natakamani_intro5 = new()
		{
			speakerImage = "NatakamaniHappy",
			speakerName = "Natakamani",
			speakerDirection = "Koa",
			speakerText = "Yes, aren't they magnificent? They serve as headstones for the graves of our greatest leaders, and I hope to some day be among my brethren " +
			"with a glorious pyramid of my own."
		};
		conversationDict["Natakamani_intro"].Add(natakamani_intro5);

		ConversationItem natakamani_intro6 = new()
		{
			speakerImage = "ScottHappy",
			speakerName = "Scott",
			speakerDirection = "Natakamani",
			speakerText = "I hope so too! I mean, when you pass away, after an appropriate amount of time, having lived a long, full life."
		};
		conversationDict["Natakamani_intro"].Add(natakamani_intro6);

		ConversationItem natakamani_intro7 = new()
		{
			speakerImage = "NatakamaniHappy",
			speakerName = "Natakamani",
			speakerDirection = "Koa",
			speakerText = "I must return to my people and my city, but do return when you get a chance. We are always keen to sell our luxurious incense which will no doubt suit the people of your kingdom."
		};
		conversationDict["Natakamani_intro"].Add(natakamani_intro7);

		#endregion
		#region quest0
		conversationDict["Natakamani_quest0"] = new();

		ConversationItem natakamani_quest01 = new()
		{
			speakerImage = "NatakamaniHappy",
			speakerName = "Natakamani",
			speakerDirection = "Koa",
			speakerText = "Greetings, friends! You have impeccable timing, we are in the process of building a glorious sun dial for our city center and " +
			"we have just noted that we have insufficient material to finish the project."
		};
		conversationDict["Natakamani_quest0"].Add(natakamani_quest01);

		ConversationItem natakamani_quest02 = new()
		{
			speakerImage = "NatakamaniQuestion",
			speakerName = "Natakamani",
			speakerDirection = "Koa",
			speakerText = "Fortunately, we need only 10 stone to help us finish. Could you perhaps bring us this amount of stone?"
		};
		conversationDict["Natakamani_quest0"].Add(natakamani_quest02);

		ConversationItem natakamani_quest03 = new()
		{
			speakerImage = "KoaQuestion",
			speakerName = "Koa",
			speakerDirection = "Natakamani",
			speakerText = "This is for a sun dial? Like to use to tell time? I keep forgetting how backwards everyone is on this world."
		};
		conversationDict["Natakamani_quest0"].Add(natakamani_quest03);

		ConversationItem natakamani_quest04 = new()
		{
			speakerImage = "ScottHappy",
			speakerName = "Scott",
			speakerDirection = "Natakamani",
			speakerText = "Koa is actually from a different world than ours! A world with super advanced technologies like flying rockets, satellites in space, " +
			"and plates made out of paper!"
		};
		conversationDict["Natakamani_quest0"].Add(natakamani_quest04);

		ConversationItem natakamani_quest05 = new()
		{
			speakerImage = "NatakamaniSurprised",
			speakerName = "Natakamani",
			speakerDirection = "Koa",
			speakerText = "You're from a different world? What is it like? What sort of technology do you have?"
		};
		conversationDict["Natakamani_quest0"].Add(natakamani_quest05);

		ConversationItem natakamani_quest06 = new()
		{
			speakerImage = "KoaQuestion",
			speakerName = "Koa",
			speakerDirection = "Natakamani",
			speakerText = "Oh, all sorts of stuff, like cars, computers, airplanes, that kind of thing."
		};
		conversationDict["Natakamani_quest0"].Add(natakamani_quest06);

		ConversationItem natakamani_quest07 = new()
		{
			speakerImage = "NatakamaniSerious",
			speakerName = "Natakamani",
			speakerDirection = "Koa",
			speakerText = "...I don't know what any of those things are."
		};
		conversationDict["Natakamani_quest0"].Add(natakamani_quest07);

		ConversationItem natakamani_quest08 = new()
		{
			speakerImage = "KoaHappy",
			speakerName = "Koa",
			speakerDirection = "Natakamani",
			speakerText = "Ok, well, for example, we have a time machine that you can wear on your wrist!"
		};
		conversationDict["Natakamani_quest0"].Add(natakamani_quest08);

		ConversationItem natakamani_quest09 = new()
		{
			speakerImage = "NatakamaniSurprised",
			speakerName = "Natakamani",
			speakerDirection = "Koa",
			speakerText = "A TIME MACHINE!? Does it allow you to travel through time, into the past or future, or does it control the very fabric of time itself, " +
			"allowing you to stop and start time at will?"
		};
		conversationDict["Natakamani_quest0"].Add(natakamani_quest09);

		ConversationItem natakamani_quest010 = new()
		{
			speakerImage = "KoaGuilty",
			speakerName = "Koa",
			speakerDirection = "Natakamani",
			speakerText = "Well, no, it just... tells you what time it is."
		};
		conversationDict["Natakamani_quest0"].Add(natakamani_quest010);

		ConversationItem natakamani_quest011 = new()
		{
			speakerImage = "NatakamaniQuestion",
			speakerName = "Natakamani",
			speakerDirection = "Koa",
			speakerText = "*Clearly disappointed* Oh. That's... interesting... Is that what you're wearing right now on your wrist? Could you tell me what time it is?"
		};
		conversationDict["Natakamani_quest0"].Add(natakamani_quest011);

		ConversationItem natakamani_quest012 = new()
		{
			speakerImage = "KoaQuestion",
			speakerName = "Koa",
			speakerDirection = "Natakamani",
			speakerText = "Yes! *Looks at watch* Oh, shoot, looks like it broke when I fell from the sky."
		};
		conversationDict["Natakamani_quest0"].Add(natakamani_quest012);

		ConversationItem natakamani_quest013 = new()
		{
			speakerImage = "NatakamaniSarcasm",
			speakerName = "Natakamani",
			speakerDirection = "Koa",
			speakerText = "Clearly you come from a civilization filled with wonder and excitement."
		};
		conversationDict["Natakamani_quest0"].Add(natakamani_quest013);

		ConversationItem natakamani_quest014 = new()
		{
			speakerImage = "ScottHappy",
			speakerName = "Scott",
			speakerDirection = "Natakamani",
			speakerText = "I know! Isn't it splendid to hear about? Anyways, we'll find stones for you, and we'll return when we do, Mr. Natakamani!"
		};
		conversationDict["Natakamani_quest0"].Add(natakamani_quest014);
		#endregion
		#region quest0complete
		conversationDict["Natakamani_quest0_complete"] = new();

		ConversationItem natakamani_quest0_complete1 = new()
		{
			speakerImage = "NatakamaniHappy",
			speakerName = "Natakamani",
			speakerDirection = "Koa",
			speakerText = "Your help on our sun dial project is much appreciated! With its completion, our people will always know the time and will be able to follow a more " +
			"structured lifestyle. And as promised, here is a reward for your help on our time-keeping project."
		};
		conversationDict["Natakamani_quest0_complete"].Add(natakamani_quest0_complete1);

		ConversationItem natakamani_quest0_complete2 = new()
		{
			speakerImage = "ScottHappy",
			speakerName = "Scott",
			speakerDirection = "Natakamani",
			speakerText = "Thanks Mr. Natakamani! We're always glad to help! So what is the sun dial for?"
		};
		conversationDict["Natakamani_quest0_complete"].Add(natakamani_quest0_complete2);

		ConversationItem natakamani_quest0_complete3 = new()
		{
			speakerImage = "NatakamaniSerious",
			speakerName = "Natakamani",
			speakerDirection = "Scott",
			speakerText = "It's... to tell us what time it is."
		};
		conversationDict["Natakamani_quest0_complete"].Add(natakamani_quest0_complete3);

		ConversationItem natakamani_quest0_complete4 = new()
		{
			speakerImage = "ScottHappy",
			speakerName = "Scott",
			speakerDirection = "Natakamani",
			speakerText = "Oh, cool!"
		};
		conversationDict["Natakamani_quest0_complete"].Add(natakamani_quest0_complete4);

		ConversationItem natakamani_quest0_complete5 = new()
		{
			speakerImage = "NatakamaniHappy",
			speakerName = "Natakamani",
			speakerDirection = "Scott",
			speakerText = "Indeed! My mother, the great Amanitore, has consistently taught to me the importance of punctuality, ensuring I use my time well and to not waste the time of others. " +
			"I'm hopeful this sun dial helps my people learn that same lesson."
		};
		conversationDict["Natakamani_quest0_complete"].Add(natakamani_quest0_complete5);

		ConversationItem natakamani_quest0_complete6 = new()
		{
			speakerImage = "NatakamaniQuestion",
			speakerName = "Natakamani",
			speakerDirection = "Koa",
			speakerText = "Also, you'll notice, Koa, that our sun dials don't break so easily. We may work with only stones, but we take great care to ensure our structures are of the utmost quality."
		};
		conversationDict["Natakamani_quest0_complete"].Add(natakamani_quest0_complete6);

		ConversationItem natakamani_quest0_complete7 = new()
		{
			speakerImage = "KoaAnnoyed",
			speakerName = "Koa",
			speakerDirection = "Natakamani",
			speakerText = "Is this about my watch? I fell from the sky! I don't think your high quality sun dials are getting you to your next appointment on time after falling from five thousand feet."
		};
		conversationDict["Natakamani_quest0_complete"].Add(natakamani_quest0_complete7);
		#endregion
		#region quest1
		conversationDict["Natakamani_quest1"] = new();

		ConversationItem natakamani_quest10 = new()
		{
			speakerImage = "NatakamaniHappy",
			speakerName = "Natakamani",
			speakerDirection = "Koa",
			speakerText = "Welcome back, friends. I hope you're well on this exceptionally warm day today."
		};
		conversationDict["Natakamani_quest1"].Add(natakamani_quest10);

		ConversationItem natakamani_quest11 = new()
		{
			speakerImage = "KoaQuestion",
			speakerName = "Koa",
			speakerDirection = "Natakamani",
			speakerText = "You know, where I'm from, we have technology that tells you everything about the weather. Like what it's going to be like later, " +
			"what it's like right now, that sort of thing."
		};
		conversationDict["Natakamani_quest1"].Add(natakamani_quest11);

		ConversationItem natakamani_quest12 = new()
		{
			speakerImage = "NatakamaniQuestion",
			speakerName = "Natakamani",
			speakerDirection = "Koa",
			speakerText = "You can't look out the window and see what the weather is? You don't have windows where you're from?"
		};
		conversationDict["Natakamani_quest1"].Add(natakamani_quest12);

		ConversationItem natakamani_quest13 = new()
		{
			speakerImage = "KoaQuestion",
			speakerName = "Koa",
			speakerDirection = "Natakamani",
			speakerText = "Um, yes, but this technology tells you the weather in advance, so you know what to expect."
		};
		conversationDict["Natakamani_quest1"].Add(natakamani_quest13);

		ConversationItem natakamani_quest14 = new()
		{
			speakerImage = "NatakamaniQuestion",
			speakerName = "Natakamani",
			speakerDirection = "Koa",
			speakerText = "I do that every day and no one I know is particularly impressed by it."
		};
		conversationDict["Natakamani_quest1"].Add(natakamani_quest14);

		ConversationItem natakamani_quest15 = new()
		{
			speakerImage = "NatakamaniSarcasm",
			speakerName = "Natakamani",
			speakerDirection = "Koa",
			speakerText = "\"Look, there's a large swathe of dark clouds coming this way, I guess that means it will rain soon.\" \"Wow, how did you know that?? Are you from the future!?\""
		};
		conversationDict["Natakamani_quest1"].Add(natakamani_quest15);

		ConversationItem natakamani_quest16 = new()
		{
			speakerImage = "NatakamaniSarcasm",
			speakerName = "Natakamani",
			speakerDirection = "Koa",
			speakerText = "\"Hey, it's really windy today, that probably indicates a storm is coming.\" \"You must be sent by the gods!!\""
		};
		conversationDict["Natakamani_quest1"].Add(natakamani_quest16);

		ConversationItem natakamani_quest17 = new()
		{
			speakerImage = "KoaSerious",
			speakerName = "Koa",
			speakerDirection = "Natakamani",
			speakerText = "*flustered* ...look, it's really cool, okay?"
		};
		conversationDict["Natakamani_quest1"].Add(natakamani_quest17);

		ConversationItem natakamani_quest18 = new()
		{
			speakerImage = "NatakamaniQuestion",
			speakerName = "Natakamani",
			speakerDirection = "Koa",
			speakerText = "Yes, it definitely sounds like a worthy use of your time."
		};
		conversationDict["Natakamani_quest1"].Add(natakamani_quest18);

		ConversationItem natakamani_quest19 = new()
		{
			speakerImage = "NatakamaniQuestion",
			speakerName = "Natakamani",
			speakerDirection = "Koa",
			speakerText = "Moving on, I don't suppose you men would you be able to get us some pottery, could you? We're thinking of using them " +
			"to decorate a memorial we are building, and we'd need about 10 of them."
		};
		conversationDict["Natakamani_quest1"].Add(natakamani_quest19);

		ConversationItem natakamani_quest110 = new()
		{
			speakerImage = "KoaSerious",
			speakerName = "Koa",
			speakerDirection = "Natakamani",
			speakerText = "*Bitterly* I guess we can try to get that for you..."
		};
		conversationDict["Natakamani_quest1"].Add(natakamani_quest110);
		#endregion
		#region quest1complete
		conversationDict["Natakamani_quest1_complete"] = new();

		ConversationItem natakamani_quest1_complete1 = new()
		{
			speakerImage = "NatakamaniHappy",
			speakerName = "Natakamani",
			speakerDirection = "Koa",
			speakerText = "I appreciate you bringing what was requested, and here is your payment, as promised. Your reward is well earned, particularly on this hot and sunny day."
		};
		conversationDict["Natakamani_quest1_complete"].Add(natakamani_quest1_complete1);

		ConversationItem natakamani_quest1_complete2 = new()
		{
			speakerImage = "NatakamaniQuestion",
			speakerName = "Natakamani",
			speakerDirection = "Koa",
			speakerText = "And do you want to know how I knew it was sunny? Do you, Koa?"
		};
		conversationDict["Natakamani_quest1_complete"].Add(natakamani_quest1_complete2);

		ConversationItem natakamani_quest1_complete3 = new()
		{
			speakerImage = "KoaAnnoyed",
			speakerName = "Koa",
			speakerDirection = "Natakamani",
			speakerText = "I'm guessing you looked outside your window and saw that it was sunny?"
		};
		conversationDict["Natakamani_quest1_complete"].Add(natakamani_quest1_complete3);

		ConversationItem natakamani_quest1_complete4 = new()
		{
			speakerImage = "NatakamaniHappy",
			speakerName = "Natakamani",
			speakerDirection = "Koa",
			speakerText = "Exactly! Yes, the presence of our windows truly makes us a top-tier civilization, most likely in the entire universe, no doubt. You must be " +
			"awe-struck with our technological capabilities."
		};
		conversationDict["Natakamani_quest1_complete"].Add(natakamani_quest1_complete4);

		ConversationItem natakamani_quest1_complete5 = new()
		{
			speakerImage = "KoaSerious",
			speakerName = "Koa",
			speakerDirection = "Natakamani",
			speakerText = "...we'll just take our reward and leave now, thanks."
		};
		conversationDict["Natakamani_quest1_complete"].Add(natakamani_quest1_complete5);
		#endregion
		#region quest2
		conversationDict["Natakamani_quest2"] = new();

		ConversationItem natakamani_quest20 = new()
		{
			speakerImage = "NatakamaniHappy",
			speakerName = "Natakamani",
			speakerDirection = "Koa",
			speakerText = "Hello again, my friends. I've noticed that your little civilization is growing rapidly " +
			"and building a high amount of large structures, I am very impressed."
		};
		conversationDict["Natakamani_quest2"].Add(natakamani_quest20);

		ConversationItem natakamani_quest21 = new()
		{
			speakerImage = "KoaQuestion",
			speakerName = "Koa",
			speakerDirection = "Natakamani",
			speakerText = "Yeah, we've been working hard over there, and I'm also getting pretty thirsty from walking around so much, I could really go for a " +
			"Mountain Dew right about now."
		};
		conversationDict["Natakamani_quest2"].Add(natakamani_quest21);

		ConversationItem natakamani_quest22 = new()
		{
			speakerImage = "NatakamaniQuestion",
			speakerName = "Natakamani",
			speakerDirection = "Koa",
			speakerText = "Mountain dew? Where you're from, you have a drink that harnesses the clear, crisp, morning dew found on the lush foliage on the highest mountains?"
		};
		conversationDict["Natakamani_quest2"].Add(natakamani_quest22);

		ConversationItem natakamani_quest23 = new()
		{
			speakerImage = "KoaGuilty",
			speakerName = "Koa",
			speakerDirection = "Natakamani",
			speakerText = "Um, yup, that's EXACTLY how it's made..."
		};
		conversationDict["Natakamani_quest2"].Add(natakamani_quest23);

		ConversationItem natakamani_quest24 = new()
		{
			speakerImage = "KoaQuestion",
			speakerName = "Koa",
			speakerDirection = "Natakamani",
			speakerText = "I'm also quite fond of a drink we have called Dr. Pepper, but I am just now realizing I shouldn't have mentioned it because you're just going " +
			"to interpret the name literally..."
		};
		conversationDict["Natakamani_quest2"].Add(natakamani_quest24);

		ConversationItem natakamani_quest25 = new()
		{
			speakerImage = "NatakamaniSurprised",
			speakerName = "Natakamani",
			speakerDirection = "Koa",
			speakerText = "You drink... pepper? The very spice that causes thirst and you drink it? What posseses you to make a drink like that? Do you also make a " +
			"drink called Professor Salty?"
		};
		conversationDict["Natakamani_quest2"].Add(natakamani_quest25);

		ConversationItem natakamani_quest26 = new()
		{
			speakerImage = "KoaGuilty",
			speakerName = "Koa",
			speakerDirection = "Natakamani",
			speakerText = "Yeah, I'm not even going to bring up Hawaiian Punch or Coke with you."
		};
		conversationDict["Natakamani_quest2"].Add(natakamani_quest26);

		ConversationItem natakamani_quest27 = new()
		{
			speakerImage = "NatakamaniQuestion",
			speakerName = "Natakamani",
			speakerDirection = "Koa",
			speakerText = "Now that you mention it, we could use those aforementioned spices. We're in the process of preparing an enormous feast. " +
			"Could you bring us something that could improve the flavor of our food? I'd like 8 of them, ideally."
		};
		conversationDict["Natakamani_quest2"].Add(natakamani_quest27);

		ConversationItem natakamani_quest28 = new()
		{
			speakerImage = "ScottHappy",
			speakerName = "Scott",
			speakerDirection = "Natakamani",
			speakerText = "Sure thing, Mr. Natakamani! I personally wouldn't know how to do that since I usually just chew on potato skins, but we'll find something for you!"
		};
		conversationDict["Natakamani_quest2"].Add(natakamani_quest28);
		#endregion
		#region quest2complete
		conversationDict["Natakamani_quest2_complete"] = new();

		ConversationItem natakamani_quest2_complete0 = new()
		{
			speakerImage = "NatakamaniHappy",
			speakerName = "Natakamani",
			speakerDirection = "Koa",
			speakerText = "Thank you for retrieving these spices for me! Considering how productive you are in your own cities, I knew I could count on you all to deliver!"
		};
		conversationDict["Natakamani_quest2_complete"].Add(natakamani_quest2_complete0);

		ConversationItem natakamani_quest2_complete1 = new()
		{
			speakerImage = "ScottHappy",
			speakerName = "Scott",
			speakerDirection = "Natakamani",
			speakerText = "You bet, Mr. Natakamani! This should definitely make your feast much tastier than my potato skins!"
		};
		conversationDict["Natakamani_quest2_complete"].Add(natakamani_quest2_complete1);

		ConversationItem natakamani_quest2_complete2 = new()
		{
			speakerImage = "NatakamaniHappy",
			speakerName = "Natakamani",
			speakerDirection = "Koa",
			speakerText = "I'm quite confident I could take your potato skins with spices like these and turn them into a meal finer than anything you've ever tasted before. " +
			"I was taught by my mother, and I am exceedingly fortunate to have grown up under the tutelate of a fantastic cook. Oh, and Koa?"
		};
		conversationDict["Natakamani_quest2_complete"].Add(natakamani_quest2_complete2);

		ConversationItem natakamani_quest2_complete2a = new()
		{
			speakerImage = "NatakamaniHappy",
			speakerName = "Natakamani",
			speakerDirection = "Koa",
			speakerText = "Oh, and Koa?"
		};
		conversationDict["Natakamani_quest2_complete"].Add(natakamani_quest2_complete2a);


		ConversationItem natakamani_quest2_complete3 = new()
		{
			speakerImage = "KoaSerious",
			speakerName = "Koa",
			speakerDirection = "Natakamani",
			speakerText = "Yes...?"
		};
		conversationDict["Natakamani_quest2_complete"].Add(natakamani_quest2_complete3);

		ConversationItem natakamani_quest2_complete4 = new()
		{
			speakerImage = "NatakamaniHappy",
			speakerName = "Natakamani",
			speakerDirection = "Koa",
			speakerText = "I've invented a new drink that you can take back and show your people. It actually quenches thirst rather than causes it, and it even allows you to carry on living for " +
			"more than a week! I call this magical drink, WATER."
		};
		conversationDict["Natakamani_quest2_complete"].Add(natakamani_quest2_complete4);

		ConversationItem natakamani_quest2_complete5 = new()
		{
			speakerImage = "KoaAnnoyed",
			speakerName = "Koa",
			speakerDirection = "Natakamani",
			speakerText = "Brilliant. Thanks. I needed that. We're going to leave now..."
		};
		conversationDict["Natakamani_quest2_complete"].Add(natakamani_quest2_complete5);
		#endregion
		#region quest3
		conversationDict["Natakamani_quest3"] = new();

		ConversationItem natakamani_quest30 = new()
		{
			speakerImage = "NatakamaniHappy",
			speakerName = "Natakamani",
			speakerDirection = "Koa",
			speakerText = "My friends! Seeing as you're from a different world with advanced technologies, might I trouble you to teach me something that could improve " +
			"our primitive world?"
		};
		conversationDict["Natakamani_quest3"].Add(natakamani_quest30);

		ConversationItem natakamani_quest31 = new()
		{
			speakerImage = "KoaQuestion",
			speakerName = "Koa",
			speakerDirection = "Natakamani",
			speakerText = "Maybe I can, what would you like to know? Like do you want to know how to make a bow and arrow? Or how to make a sharp stick?"
		};
		conversationDict["Natakamani_quest3"].Add(natakamani_quest31);

		ConversationItem natakamani_quest32 = new()
		{
			speakerImage = "NatakamaniQuestion",
			speakerName = "Natakamani",
			speakerDirection = "Koa",
			speakerText = "It would be enormously useful to render myself completely invisible to anyone looking in my direction, thus creating the perfect hunter. " +
			"Could you pass along that technology to me?"
		};
		conversationDict["Natakamani_quest3"].Add(natakamani_quest32);

		ConversationItem natakamani_quest33 = new()
		{
			speakerImage = "KoaGuilty",
			speakerName = "Koa",
			speakerDirection = "Natakamani",
			speakerText = "Oh. Uh... nope, can't do that, haven't figured that one out yet."
		};
		conversationDict["Natakamani_quest3"].Add(natakamani_quest33);

		ConversationItem natakamani_quest34 = new()
		{
			speakerImage = "NatakamaniQuestion",
			speakerName = "Natakamani",
			speakerDirection = "Koa",
			speakerText = "Well, what about the ability to shift my entire physical body to a different location, miles away, in a split second. Could you teach me to do that?"
		};
		conversationDict["Natakamani_quest3"].Add(natakamani_quest34);

		ConversationItem natakamani_quest35 = new()
		{
			speakerImage = "KoaQuestion",
			speakerName = "Koa",
			speakerDirection = "Natakamani",
			speakerText = "Oh gosh, no, we can't do that one either. I have absolutely no idea how that one would even work... maybe I can teach you how to whistle really loudly instead? " +
			"Maybe that's just as cool?"
		};
		conversationDict["Natakamani_quest3"].Add(natakamani_quest35);

		ConversationItem natakamani_quest36 = new()
		{
			speakerImage = "NatakamaniSarcasm",
			speakerName = "Natakamani",
			speakerDirection = "Koa",
			speakerText = "It seems your superior technology is severly lacking in practical application."
		};
		conversationDict["Natakamani_quest3"].Add(natakamani_quest36);

		ConversationItem natakamani_quest37 = new()
		{
			speakerImage = "NatakamaniQuestion",
			speakerName = "Natakamani",
			speakerDirection = "Koa",
			speakerText = "Well, perhaps you can help me in this particular task. We are building another pyramid, but are in need of building material larger than regular stones or " +
			"bricks. Could you provide 5 large stone slabs?"
		};
		conversationDict["Natakamani_quest3"].Add(natakamani_quest37);

		ConversationItem natakamani_quest38 = new()
		{
			speakerImage = "ScottHappy",
			speakerName = "Scott",
			speakerDirection = "Natakamani",
			speakerText = "That one we can do! We'll be back in a jiffy with that item you want!"
		};
		conversationDict["Natakamani_quest3"].Add(natakamani_quest38);
		#endregion
		#region quest3complete
		conversationDict["Natakamani_quest3_complete"] = new();

		ConversationItem natakamani_quest3_complete0 = new()
		{
			speakerImage = "NatakamaniHappy",
			speakerName = "Natakamani",
			speakerDirection = "Koa",
			speakerText = "Thank you for getting this for me, even if you did have to manually walk to retrieve it."
		};
		conversationDict["Natakamani_quest3_complete"].Add(natakamani_quest3_complete0);

		ConversationItem natakamani_quest3_complete1 = new()
		{
			speakerImage = "KoaQuestion",
			speakerName = "Koa",
			speakerDirection = "Natakamani",
			speakerText = "Yeah, you bet. You know, in all seriousness, if you figure out how to teleport people or items, could you let me know? All this walking around is exhausting."
		};
		conversationDict["Natakamani_quest3_complete"].Add(natakamani_quest3_complete1);

		ConversationItem natakamani_quest3_complete2 = new()
		{
			speakerImage = "NatakamaniHappy",
			speakerName = "Natakamani",
			speakerDirection = "Koa",
			speakerText = "When I think about it, perhaps a machine like the one you described is unnecessary, for the journey itself is what builds a man, not the arrival at the destination. " +
			"That is a lesson my mother taught to me years ago."
		};
		conversationDict["Natakamani_quest3_complete"].Add(natakamani_quest3_complete2);

		ConversationItem natakamani_quest3_complete3 = new()
		{
			speakerImage = "KoaQuestion",
			speakerName = "Koa",
			speakerDirection = "Natakamani",
			speakerText = "Well, with all due respect to your mother, but she's never been dropped from the sky and needed to recreate all of human history..."
		};
		conversationDict["Natakamani_quest3_complete"].Add(natakamani_quest3_complete3);
		#endregion
		#region quest4
		conversationDict["Natakamani_quest4"] = new();

		ConversationItem natakamani_quest40 = new()
		{
			speakerImage = "NatakamaniHappy",
			speakerName = "Natakamani",
			speakerDirection = "Koa",
			speakerText = "My friends, I have a final request, I am in need of some diamonds, could you help me with that? I would need 10 in total. " +
			"I'm hoping to use them to create jewelry for my mother, if it's not too hard to do."
		};
		conversationDict["Natakamani_quest4"].Add(natakamani_quest40);

		ConversationItem natakamani_quest41 = new()
		{
			speakerImage = "KoaQuestion",
			speakerName = "Koa",
			speakerDirection = "Natakamani",
			speakerText = "Yeah, I don't think it would be too hard, I did it in a video game once."
		};
		conversationDict["Natakamani_quest4"].Add(natakamani_quest41);

		ConversationItem natakamani_quest42 = new()
		{
			speakerImage = "NatakamaniQuestion",
			speakerName = "Natakamani",
			speakerDirection = "Koa",
			speakerText = "You did it in a \"what\" game?"
		};
		conversationDict["Natakamani_quest4"].Add(natakamani_quest42);

		ConversationItem natakamani_quest43 = new()
		{
			speakerImage = "KoaQuestion",
			speakerName = "Koa",
			speakerDirection = "Natakamani",
			speakerText = "Oh geez, I don't know how to explain this one. A video game is a game that you play, but it's not built out of physical objects, " +
			"but instead out of, uh, light, which is arranged into images, and you can control the images and move them around."
		};
		conversationDict["Natakamani_quest4"].Add(natakamani_quest43);

		ConversationItem natakamani_quest44 = new()
		{
			speakerImage = "NatakamaniSurprised",
			speakerName = "Natakamani",
			speakerDirection = "Koa",
			speakerText = "Your world has found the ability to harness light, control it, and create images out of it? And you use it for entertainment?"
		};
		conversationDict["Natakamani_quest4"].Add(natakamani_quest44);

		ConversationItem natakamani_quest45 = new()
		{
			speakerImage = "KoaQuestion",
			speakerName = "Koa",
			speakerDirection = "Natakamani",
			speakerText = "Yeah, I guess. Well, we also use it to pass along information and communicate over long distances, but I mostly use it play games " +
			"and watch programs and stuff."
		};
		conversationDict["Natakamani_quest4"].Add(natakamani_quest45);

		ConversationItem natakamani_quest46 = new()
		{
			speakerImage = "NatakamaniSurprised",
			speakerName = "Natakamani",
			speakerDirection = "Koa",
			speakerText = "That's amazing actually, you've harnessed light! You've taken a vital ingredient of life and used it to further develop " +
			"your people! And I trust your people use it solely with good intentions?"
		};
		conversationDict["Natakamani_quest4"].Add(natakamani_quest46);

		ConversationItem natakamani_quest47 = new()
		{
			speakerImage = "KoaGuilty",
			speakerName = "Koa",
			speakerDirection = "Natakamani",
			speakerText = "Um... correct! People on my planet definitely don't use it for any bad things... "
		};
		conversationDict["Natakamani_quest4"].Add(natakamani_quest47);
		#endregion
		#region quest4complete
		conversationDict["Natakamani_quest4_complete"] = new();

		ConversationItem natakamani_quest4_complete0 = new()
		{
			speakerImage = "NatakamaniHappy",
			speakerName = "Natakamani",
			speakerDirection = "Koa",
			speakerText = "Thank you, my friends! These precious stones are just what I needed to finish the jewelry."
		};
		conversationDict["Natakamani_quest4_complete"].Add(natakamani_quest4_complete0);

		ConversationItem natakamani_quest4_complete1 = new()
		{
			speakerImage = "ScottHappy",
			speakerName = "Scott",
			speakerDirection = "Natakamani",
			speakerText = "So why did you choose these specific stones to use in the jewelry you're making for your mother?"
		};
		conversationDict["Natakamani_quest4_complete"].Add(natakamani_quest4_complete1);

		ConversationItem natakamani_quest4_complete2 = new()
		{
			speakerImage = "NatakamaniQuestion",
			speakerName = "Natakamani",
			speakerDirection = "Koa",
			speakerText = "Diamonds were always my mother's favorite of all of the precious stones of this land. She always said the brightness of each diamond " +
			"reminded her of the light in every soul."
		};
		conversationDict["Natakamani_quest4_complete"].Add(natakamani_quest4_complete2);

		ConversationItem natakamani_quest4_complete3 = new()
		{
			speakerImage = "KoaQuestion",
			speakerName = "Koa",
			speakerDirection = "Natakamani",
			speakerText = "Were her favorite? She doesn't like them anymore? Or..."
		};
		conversationDict["Natakamani_quest4_complete"].Add(natakamani_quest4_complete3);

		ConversationItem natakamani_quest4_complete4 = new()
		{
			speakerImage = "NatakamaniSad",
			speakerName = "Natakamani",
			speakerDirection = "Koa",
			speakerText = "Alas, Amanitore, my dear mother, she is no longer with us. Did I not make that clear in the beginning? Indeed, all of the tasks that you have " +
			"helped me with were for the burial ceremony."
		};
		conversationDict["Natakamani_quest4_complete"].Add(natakamani_quest4_complete4);

		ConversationItem natakamani_quest4_complete5 = new()
		{
			speakerImage = "NatakamaniSad",
			speakerName = "Natakamani",
			speakerDirection = "Koa",
			speakerText = "The sun dial was built to honor her, the memorial with the pottery was hers, the spices were for the final feast in her name, the stone slabs " +
			"were to create her pyramid headstone, and the diamonds are for the jewelry to adorn her tomb, to be with her eternally."
		};
		conversationDict["Natakamani_quest4_complete"].Add(natakamani_quest4_complete5);

		ConversationItem natakamani_quest4_complete6 = new()
		{
			speakerImage = "KoaSad",
			speakerName = "Koa",
			speakerDirection = "Natakamani",
			speakerText = "Oh, we had no idea, I'm sorry about your loss. I hope that we were helpful to you during this trying time."
		};
		conversationDict["Natakamani_quest4_complete"].Add(natakamani_quest4_complete6);

		ConversationItem natakamani_quest4_complete7 = new()
		{
			speakerImage = "NatakamaniHappy",
			speakerName = "Natakamani",
			speakerDirection = "Koa",
			speakerText = "Oh, you were very helpful! I know I tried to keep our conversations light at times, but I am very grateful for what you did."
		};
		conversationDict["Natakamani_quest4_complete"].Add(natakamani_quest4_complete7);

		ConversationItem natakamani_quest4_complete8 = new()
		{
			speakerImage = "NatakamaniHappy",
			speakerName = "Natakamani",
			speakerDirection = "Koa",
			speakerText = "I am glad that we met, Koa! For without you and your services, I'm not sure we would ever have been able to give her the proper ceremony she deserved. " +
			"With that said, here is your reward, and I am tremendously grateful for the work you have done for us."
		};
		conversationDict["Natakamani_quest4_complete"].Add(natakamani_quest4_complete8);
		#endregion
		#endregion

		#region Sennacherib
		#region intro
		conversationDict["Sennacherib_intro"] = new();

		ConversationItem sennacherib_intro1 = new()
		{
			speakerImage = "SennacheribMad",
			speakerName = "Sennacherib",
			speakerDirection = "Koa",
			speakerText = "Greetings! I am Sennacherib, noble warrior and king of the Assyrians! With my mighty armies, I have conquered and destroyed the mighty city of Babylon."
		};
		conversationDict["Sennacherib_intro"].Add(sennacherib_intro1);

		ConversationItem sennacherib_intro2 = new()
		{
			speakerImage = "SennacheribMad",
			speakerName = "Sennacherib",
			speakerDirection = "Koa",
			speakerText = "Behold, my capital city of Ninevah! Observe these gargantuan city walls and the expanse of the city, all of which were constructed through my leadership."
		};
		conversationDict["Sennacherib_intro"].Add(sennacherib_intro2);

		ConversationItem sennacherib_intro3 = new()
		{
			speakerImage = "SennacheribMad",
			speakerName = "Sennacherib",
			speakerDirection = "Koa",
			speakerText = "And what of your accomplishments? Have you achieved even a fraction of what I have?"
		};
		conversationDict["Sennacherib_intro"].Add(sennacherib_intro3);

		ConversationItem sennacherib_intro4 = new()
		{
			speakerImage = "KoaQuestion",
			speakerName = "Koa",
			speakerDirection = "Sennacherib",
			speakerText = "Um, yeah, I have lots of accomplishments! For example, I'm a high school graduate *cough* equivalent, I can tell the difference between real bacon and tofu bacon..." +
			" um, I've also seen an elephant... "
		};
		conversationDict["Sennacherib_intro"].Add(sennacherib_intro4);

		ConversationItem sennacherib_intro5 = new()
		{
			speakerImage = "KoaGuilty",
			speakerName = "Koa",
			speakerDirection = "Sennacherib",
			speakerText = "Do you want me to keep going? Because I could totally keep going. Also, what kind of name is Sennacherib? More like, \"snack a rib\", am I right?"
		};
		conversationDict["Sennacherib_intro"].Add(sennacherib_intro5);

		ConversationItem sennacherib_intro6 = new()
		{
			speakerImage = "SennacheribMad",
			speakerName = "Sennacherib",
			speakerDirection = "Koa",
			speakerText = "...it is clear to me that I must destroy you now. Your piffling civilization is consuming resources that are rightfully ours, and we are prepared to fight to take them back."
		};
		conversationDict["Sennacherib_intro"].Add(sennacherib_intro6);

		ConversationItem sennacherib_intro7 = new()
		{
			speakerImage = "SennacheribMad",
			speakerName = "Sennacherib",
			speakerDirection = "Koa",
			speakerText = "Return to your houses, and prepare yourselves, for we are coming for you. When the birds go silent and your dogs cower in fear, that will be the sound of our arrival."
		};
		conversationDict["Sennacherib_intro"].Add(sennacherib_intro7);
		#endregion
		#region challenge0
		conversationDict["Sennacherib_challenge0"] = new();

		ConversationItem sennacherib_challenge01 = new()
		{
			speakerImage = "SennacheribMad",
			speakerName = "Sennacherib",
			speakerDirection = "Koa",
			speakerText = "I have sent vast armies to destroy the mighty city of Babylon, but I am not an idle leader who will have his subjects do all of his fighting. " +
			"I challenge any one of you to a duel at a time of your choosing."
		};
		conversationDict["Sennacherib_challenge0"].Add(sennacherib_challenge01);

		ConversationItem sennacherib_challenge02 = new()
		{
			speakerImage = "KoaSerious",
			speakerName = "Koa",
			speakerDirection = "Sennacherib",
			speakerText = "You want to fight me? Is this because of my \"snack a rib\" joke from earlier?"
		};
		conversationDict["Sennacherib_challenge0"].Add(sennacherib_challenge02);

		ConversationItem sennacherib_challenge03 = new()
		{
			speakerImage = "AzaiSerious",
			speakerName = "Azai",
			speakerDirection = "Koa",
			speakerText = "I think this is a challenge that is more appropriately suited for me. I have received combat training for my entire life to prepare myself for duels such as these."
		};
		conversationDict["Sennacherib_challenge0"].Add(sennacherib_challenge03);

		ConversationItem sennacherib_challenge04 = new()
		{
			speakerImage = "KoaQuestion",
			speakerName = "Koa",
			speakerDirection = "Azai",
			speakerText = "Are you sure? I think I can take him, I worked out my chest the other day so I'm feeling pretty huge."
		};
		conversationDict["Sennacherib_challenge0"].Add(sennacherib_challenge04);

		ConversationItem sennacherib_challenge05 = new()
		{
			speakerImage = "AzaiSerious",
			speakerName = "Azai",
			speakerDirection = "Koa",
			speakerText = "I assure you, leave this one to me."
		};
		conversationDict["Sennacherib_challenge0"].Add(sennacherib_challenge05);
		#endregion
		#region challenge1
		conversationDict["Sennacherib_challenge1"] = new();

		ConversationItem sennacherib_challenge11 = new()
		{
			speakerImage = "SennacheribMad",
			speakerName = "Sennacherib",
			speakerDirection = "Koa",
			speakerText = "So you return to challenge me again? Have you prepared your soul to face your inevitable defeat?"
		};
		conversationDict["Sennacherib_challenge1"].Add(sennacherib_challenge11);
		#endregion
		#region 
		conversationDict["Sennacherib_defeat"] = new();

		ConversationItem sennacherib_defeat1 = new()
		{
			speakerImage = "KoaHappy",
			speakerName = "Koa",
			speakerDirection = "Azai",
			speakerText = "Thanks for handling that guy, Azai, although I think I could have taken him if I had a new sword like you have. And if I was taller. And stronger. And knew how to fight."
		};
		conversationDict["Sennacherib_defeat"].Add(sennacherib_defeat1);

		ConversationItem sennacherib_defeat2 = new()
		{
			speakerImage = "AzaiSerious",
			speakerName = "Azai",
			speakerDirection = "Koa",
			speakerText = "He was a powerful adversary with a heart filled with destruction. It is better for the world with him removed."
		};
		conversationDict["Sennacherib_defeat"].Add(sennacherib_defeat2);

		ConversationItem sennacherib_defeat3 = new()
		{
			speakerImage = "ScottSerious",
			speakerName = "Scott",
			speakerDirection = "Koa",
			speakerText = "\"For the Angel of Death spread his wings on the blast, and breathed in the face of the foe as he passed, and the eyes of the sleepers waxed deadly and chill, " +
			"and their hearts but once heaved, and for ever grew still.\""
		};
		conversationDict["Sennacherib_defeat"].Add(sennacherib_defeat3);

		ConversationItem sennacherib_defeat4 = new()
		{
			speakerImage = "ScottHappy",
			speakerName = "Scott",
			speakerDirection = "Koa",
			speakerText = "That's Lord Byron!"
		};
		conversationDict["Sennacherib_defeat"].Add(sennacherib_defeat4);

		ConversationItem sennacherib_defeat5 = new()
		{
			speakerImage = "KoaConfused",
			speakerName = "Koa",
			speakerDirection = "Scott",
			speakerText = "How do you know who Lord Byron is?"
		};
		conversationDict["Sennacherib_defeat"].Add(sennacherib_defeat5);
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
