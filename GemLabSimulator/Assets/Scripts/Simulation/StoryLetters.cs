using System.Collections.Generic;

// The letters of the story, written by the characters. They arrive in the personal mailbox of the house and can be read again there.
public class StoryLetter
{
    public string Id;
    public string Title;
    public string From;
    public string Body;
}

public static class StoryLetters
{
    public const string Paris = "paris";
    public const string Welcome = "welcome";

    public static readonly StoryLetter[] All =
    {
        new StoryLetter
        {
            Id = Paris, Title = "La lettre de Paris", From = "Vincent Pardieu",
            Body =
                "À toi,\n\n" +
                "Je dois partir quelque temps, me mettre à l'abri. Je ne reviendrai peut-être jamais.\n\n" +
                "Je ne peux pas t'en dire davantage par écrit : certaines lettres se perdent, et d'autres se lisent. Retiens seulement ceci : le laboratoire de Crystal Grove est à toi. J'ai tout arrangé avec le notaire.\n\n" +
                "Fais-le tourner. Et protège Crystal Grove : cette ville et ses pierres comptent plus que tu ne le crois.\n\n" +
                "J'ai confiance en toi.\n\n" +
                "Vincent",
        },
        new StoryLetter
        {
            Id = Welcome, Title = "Bienvenue à Crystal Grove", From = "Vincent Pardieu",
            Body =
                "À toi,\n\n" +
                "Si tu lis ces lignes, tu as réussi à rejoindre Crystal Grove, et j'en suis heureux. Ta maison est petite, mais elle est à toi.\n\n" +
                "La professeure Thouvenot t'a sûrement fait faire le tour de la ville. Elle est superbe, n'est-ce pas ? Fie-toi à elle : elle en sait bien plus qu'elle ne le dit.\n\n" +
                "<b>Les bases</b>\n" +
                "· Marche avec les flèches du clavier. La touche E sert à tout : parler, ouvrir une porte ou un meuble, utiliser un instrument.\n" +
                "· Le sac (touche H) contient tes pierres et tes outils. Ses onglets sont le calendrier (K), le grimoire des pierres (G), et plus tard tes livres.\n" +
                "· Un clic sur la petite carte de la ville en ouvre une grande.\n" +
                "· Dors dans ton lit (E) : la journée passe et la partie est sauvegardée. Le menu de pause (Échap) règle la musique et te ramène au menu principal, en sauvegardant aussi.\n\n" +
                "<b>Le bureau de gemmologie</b>\n" +
                "· Les gens t'écrivent : les lettres arrivent dans la boîte aux lettres du bureau, avec une pierre à identifier. Examine-la avec les instruments classiques, note ce que tu observes, puis remets ton rapport au directeur.\n" +
                "· Un bon rapport te rapporte de l'argent et de la réputation. Avec la réputation, de nouveaux lieux de la ville s'ouvriront : le musée, le manoir, le grand laboratoire et ses instruments complexes, le quartier des diamantaires, la banque...\n" +
                "· Le GemStore vend les instruments. Commence par une loupe.\n\n" +
                "<b>Une dernière chose</b>\n" +
                "Si la porte de ma maison reste fermée, ne t'en étonne pas, et ne la force pas. Un jour, tu comprendras pourquoi.\n\n" +
                "Prends soin de toi, et regarde bien avant de croire.\n\n" +
                "Vincent",
        },
    };

    public static StoryLetter Find(string id)
    {
        foreach (var letter in All) if (letter.Id == id) return letter;
        return null;
    }
}
