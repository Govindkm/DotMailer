namespace DotMailer.Templates;

/// <summary>
/// Renders email content from a template and a model.
/// </summary>
public interface IEmailTemplateRenderer
{
    Task<string> RenderAsync<TModel>(string templateName, TModel model, CancellationToken cancellationToken = default);
}
