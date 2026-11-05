using UnityEngine;

// The kinds of mail of the laboratory, each with its own envelope: the donations, the stones of the clients to identify, the letters of the
// characters of the town (cream, for later) and the weekly catalogue of the auctions (gold).
public enum LetterKind { Donation, Client, Npc, Catalogue }

public static class MailLetters
{
    public static string KindLabel(LetterKind kind)
    {
        switch (kind)
        {
            case LetterKind.Donation: return "Don anonyme";
            case LetterKind.Client: return "Pierre à identifier";
            case LetterKind.Npc: return "Courrier personnel";
            default: return "Bulletin des ventes";
        }
    }

    public static Color BodyColor(LetterKind kind)
    {
        switch (kind)
        {
            case LetterKind.Donation: return new Color(0.50f, 0.76f, 0.56f);
            case LetterKind.Client: return new Color(0.52f, 0.68f, 0.88f);
            case LetterKind.Npc: return new Color(0.97f, 0.94f, 0.85f);
            default: return new Color(0.94f, 0.78f, 0.32f);
        }
    }

    public static Color FlapColor(LetterKind kind) => Color.Lerp(BodyColor(kind), new Color(0.2f, 0.15f, 0.1f), 0.22f);

    public static Color SealColor(LetterKind kind)
    {
        switch (kind)
        {
            case LetterKind.Donation: return new Color(0.95f, 0.82f, 0.30f);
            case LetterKind.Client: return new Color(0.80f, 0.25f, 0.25f);
            case LetterKind.Npc: return new Color(0.62f, 0.20f, 0.24f);
            default: return new Color(0.62f, 0.14f, 0.16f);
        }
    }

    // The paper of an opened letter takes a light tint of its envelope.
    public static Color PaperColor(LetterKind kind) => Color.Lerp(new Color(0.97f, 0.94f, 0.86f), BodyColor(kind), kind == LetterKind.Npc ? 0f : 0.10f);

    private static readonly string[] Reasons =
    {
        "Cette pierre appartenait à ma grand-mère. Je ne sais pas ce que c'est, ni si elle a de la valeur.",
        "Je l'ai achetée sur un marché pendant mes vacances. Le vendeur m'a juré qu'elle était naturelle : je voudrais en avoir le cœur net.",
        "Mon bijoutier pense qu'elle a pu être traitée, mais il n'a pas voulu m'en dire plus. Pourriez-vous vérifier ?",
        "J'hérite de cette pierre et je dois la faire estimer pour la succession.",
        "Je la porte en bague depuis vingt ans et j'aimerais savoir ce que j'ai au doigt.",
        "Ma compagnie d'assurance exige une identification précise de cette pierre avant de l'assurer.",
        "Je l'ai trouvée en bêchant mon potager. Est-ce une vraie pierre, ou un caillou poli par le temps ?",
        "Je collectionne les minéraux et je veux confirmer l'espèce de cette pièce avant de l'inscrire à mon catalogue.",
        "Un ami me l'a offerte pour mon anniversaire. Est-elle vraie, ou de synthèse ?",
        "Je prépare un cadeau de mariage et je voudrais être certain de la nature de la pierre avant de la faire sertir.",
        "On m'a proposé de la revendre et je ne sais pas à quel prix. Une identification sérieuse m'aiderait à négocier.",
        "Elle dormait au fond d'un tiroir depuis des années. J'aimerais enfin savoir ce qu'elle est.",
    };

    private static readonly string[] RoughReasons =
    {
        "J'ai trouvé ce cristal dans le lit d'une rivière, en promenade. Il brille quand on le mouille : est-ce une vraie pierre ?",
        "Il vient de la mine de mon grand-père, fermée depuis longtemps. Personne dans la famille ne sait ce que c'est.",
        "Je l'ai ramassé lors d'une excursion en montagne. Je ne l'ai pas fait tailler : je voudrais d'abord savoir ce que c'est.",
        "Un prospecteur me l'a vendu comme une pierre précieuse brute. Je voudrais savoir si je me suis fait avoir.",
        "Mon fils l'a rapporté d'un voyage. Il est tel qu'il a été trouvé, sans aucun polissage.",
        "Il traînait au fond d'une caisse de minéraux achetée à une brocante. Je suis curieux de savoir ce qu'il vaut.",
    };

    private static readonly string[] Donations =
    {
        "Chers gemmologues,\n\nJe fais don de cette pierre à votre laboratoire, pour vos recherches et pour votre collection. Elle vient d'un héritage que je ne peux garder.\n\nConservez-la une fois identifiée : elle sera mieux chez vous.\n\nUn mécène qui souhaite rester anonyme.",
        "À l'attention du laboratoire,\n\nCette pierre n'a rien à faire dans un tiroir. Je vous la confie pour que vous l'étudiiez et que vous la montriez, si elle le mérite.\n\nAucune réponse n'est attendue. Merci pour votre travail.\n\nUn ami du laboratoire.",
        "Madame, Monsieur,\n\nUne pierre pour vos vitrines, avec toute ma gratitude pour ce que vous faites pour la gemmologie.\n\nDonnez-lui un nom, et gardez-la.\n\nUn mécène anonyme.",
    };

    // The message that comes with a letter: the request of the client, or the few words of a donor. The same stone always has the same message.
    public static string MessageFor(GemInstance gem)
    {
        var hash = StoneOptics.StableHash((gem.SpecimenId ?? "") + "|mail");
        if (gem.IsDonation) return Donations[(int)(hash % (uint)Donations.Length)];

        if (gem.IsRough)
        {
            var roughReason = RoughReasons[(int)(hash % (uint)RoughReasons.Length)];
            return $"Madame, Monsieur,\n\n{roughReason}\n\nVous trouverez le cristal brut dans le pli joint. Il n'est pas taillé : faites ce qu'il faut pour l'examiner. Pourriez-vous l'identifier et m'envoyer un rapport ? Je vous en réglerai les frais, {gem.IdentificationReportFee} €, à réception.\n\nBien cordialement,\n{gem.ClientName}";
        }

        var reason = Reasons[(int)(hash % (uint)Reasons.Length)];
        return $"Madame, Monsieur,\n\n{reason}\n\nVous trouverez la pierre dans le pli joint. Pourriez-vous l'identifier et m'envoyer un rapport ? Je vous en réglerai les frais, {gem.IdentificationReportFee} €, à réception.\n\nBien cordialement,\n{gem.ClientName}";
    }

    // The first words of the message, for the list.
    public static string Preview(GemInstance gem)
    {
        if (gem.IsDonation) return "« Je fais don de cette pierre à votre laboratoire... »";
        var message = MessageFor(gem);
        var start = message.IndexOf("\n\n") + 2;
        var end = message.IndexOf("\n\n", start);
        var line = end > start ? message.Substring(start, end - start) : message;
        return line.Length > 78 ? "« " + line.Substring(0, 76).TrimEnd() + "... »" : "« " + line + " »";
    }

    public static string CatalogueMessage(System.DateTime sale, string theme, string description, int issue)
    {
        return $"Bulletin des ventes n° {issue}\n\nLa Banque de la Ville a le plaisir de vous adresser le catalogue de sa vente aux enchères du {GameCalendar.Long(sale)}.\n\nThème de la vente : {theme}.\n{description}\n\nLes lots, leurs estimations et leurs mises à prix sont dans l'onglet « Enchères » de votre sac.\n\nLa salle des ventes vous accueille de 9h à 20h.";
    }
}
