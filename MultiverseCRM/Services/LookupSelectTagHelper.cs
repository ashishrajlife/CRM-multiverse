using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace MultiverseCRM.Services;

[HtmlTargetElement("lookup-select")]
public class LookupSelectTagHelper : TagHelper
{
    private readonly ILookupService _lookups;
    public LookupSelectTagHelper(ILookupService lookups) => _lookups = lookups;

    public string Category { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Placeholder { get; set; }
    public string? Selected { get; set; }
    public bool Staff { get; set; }   // true = options come from Users table

    public override async Task ProcessAsync(TagHelperContext ctx, TagHelperOutput output)
    {
        var e = HtmlEncoder.Default;
        output.TagName = "select";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("name", Name);
        if (!output.Attributes.ContainsName("class")) output.Attributes.SetAttribute("class", "form-select");

        var sb = new StringBuilder();
        if (Placeholder != null) sb.Append($"<option value=\"\">{e.Encode(Placeholder)}</option>");

        if (Staff)
            foreach (var s in await _lookups.StaffAsync())
                sb.Append($"<option value=\"{e.Encode(s)}\"{(s == Selected ? " selected" : "")}>{e.Encode(s)}</option>");
        else
            foreach (var o in await _lookups.GetAsync(Category))
            {
                var sel = Selected != null ? o.Value == Selected : o.IsDefault;
                sb.Append($"<option value=\"{e.Encode(o.Value)}\"{(sel ? " selected" : "")}>{e.Encode(o.Label ?? o.Value)}</option>");
            }
        output.Content.SetHtmlContent(sb.ToString());
    }
}