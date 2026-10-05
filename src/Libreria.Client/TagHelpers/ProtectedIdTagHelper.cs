using System;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Libreria.Application.Ports.Primary;

namespace Libreria.Client.TagHelpers
{
    [HtmlTargetElement("a", Attributes = "asp-protected-id")]
    public class ProtectedIdTagHelper : TagHelper
    {
        private readonly IUrlProtector _protector;

        public ProtectedIdTagHelper(IUrlProtector protector)
        {
            _protector = protector;
        }

        [HtmlAttributeName("asp-protected-id")]
        public Guid ProtectedId { get; set; }

   
        public override int Order => 0;

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
   
            var cifrado = _protector.Cifrar(ProtectedId);
            
   
            var cifradoUrl = Uri.EscapeDataString(cifrado);
            
   
            var hrefAttr = output.Attributes["href"];
            var hrefBase = hrefAttr != null ? hrefAttr.Value.ToString() : "";
            
   
            var separador = hrefBase.Contains("?") ? "&" : "?";
            output.Attributes.SetAttribute("href", $"{hrefBase}{separador}id={cifradoUrl}");
        }
    }
}
