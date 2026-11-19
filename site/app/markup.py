"""Simple, safe text formatting for forum messages and lessons.
Everything is escaped first; only a few patterns are turned back into HTML."""
import html
import re

_URL = re.compile(r"(https?://[^\s<>\"']+)")


def count_links(text):
    return len(_URL.findall(text))


def _inline(escaped):
    out = re.sub(r"`([^`\n]+)`", r"<code>\1</code>", escaped)
    out = re.sub(r"\*\*([^*\n]+)\*\*", r"<strong>\1</strong>", out)
    out = re.sub(r"(?<![\w*])\*([^*\n]+)\*(?![\w*])", r"<em>\1</em>", out)

    def link(m):
        url = m.group(1)
        trail = ""
        while url and url[-1] in ".,;:!?)":
            trail = url[-1] + trail
            url = url[:-1]
        return '<a href="{0}" rel="nofollow ugc noopener noreferrer" target="_blank">{0}</a>{1}'.format(url, trail)
    return _URL.sub(link, out)


def forum_html(text):
    """Paragraphs, **bold**, *italic*, `code`, '- ' lists, '> ' quotes, automatic links."""
    text = text.replace("\r\n", "\n").replace("\r", "\n").strip()
    blocks = re.split(r"\n{2,}", text)
    out = []
    for block in blocks:
        lines = block.split("\n")
        if all(l.startswith("- ") for l in lines):
            items = "".join("<li>{}</li>".format(_inline(html.escape(l[2:]))) for l in lines)
            out.append("<ul>{}</ul>".format(items))
        elif all(l.startswith("&gt; ") or l.startswith("> ") for l in lines):
            inner = "<br>".join(_inline(html.escape(l[2:])) for l in lines)
            out.append("<blockquote>{}</blockquote>".format(inner))
        else:
            out.append("<p>{}</p>".format("<br>".join(_inline(html.escape(l)) for l in lines)))
    return "\n".join(out)


def lesson_html(text):
    """Lesson texts use \\n for paragraphs and <b> / <i> for emphasis (nothing else is allowed)."""
    escaped = html.escape(text.strip(), quote=False)
    for tag in ("b", "i"):
        escaped = escaped.replace("&lt;{}&gt;".format(tag), "<{}>".format(tag))
        escaped = escaped.replace("&lt;/{}&gt;".format(tag), "</{}>".format(tag))
    paras = [p.strip() for p in re.split(r"\n{2,}", escaped) if p.strip()]
    return "\n".join("<p>{}</p>".format(p.replace("\n", "<br>")) for p in paras)


_TAG = re.compile(r"(<[^>]+>)")


def link_terms(html_text, seen):
    """Turns the first mention of each glossary term into a link to the glossary. `seen` (a set of term ids) is shared
    by every text of the same page, so a term is linked once per page. The text is already escaped; tags are left alone."""
    from . import catalog
    _, by_id, pattern, forms = catalog.glossary()
    if pattern is None:
        return html_text
    inside_link = [False]

    def repl(match):
        word = match.group(1)
        tid = forms.get(word.lower())
        if not tid or tid in seen:
            return word
        seen.add(tid)
        return '<a class="gloss" href="/glossaire#{}" title="{}">{}</a>'.format(tid, html.escape(by_id[tid]["def"]), word)

    out = []
    for part in _TAG.split(html_text):
        if part.startswith("<"):
            low = part.lower()
            if low.startswith("<a ") or low == "<a>":
                inside_link[0] = True
            elif low == "</a>":
                inside_link[0] = False
            out.append(part)
        else:
            out.append(part if inside_link[0] else pattern.sub(repl, part))
    return "".join(out)
