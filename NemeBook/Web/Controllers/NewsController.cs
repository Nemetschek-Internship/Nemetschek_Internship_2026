using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces.News;
using Web.ViewModels;

namespace Web.Controllers;

[Authorize]
public class NewsController : Controller
{
    private const int NewsPreviewLength = 140;

    private readonly INewsService newsService;

    public NewsController(INewsService newsService)
    {
        this.newsService = newsService;
    }

    [HttpGet("/News")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
    {
        var viewModel = await BuildViewModelAsync(cancellationToken);
        viewModel.SuccessMessage = TempData["NewsSuccess"] as string;
        viewModel.ErrorMessage = TempData["NewsError"] as string;

        return View(viewModel);
    }

    [HttpPost]
    [Authorize(Roles = "Principal")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        NewsIndexViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            var viewModel = await BuildViewModelAsync(cancellationToken);
            viewModel.Title = model.Title;
            viewModel.Text = model.Text;
            return View("Index", viewModel);
        }

        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return Challenge();
        }

        await newsService.CreateNewsAsync(
            model.Title,
            model.Text,
            currentUserId.Value,
            cancellationToken);

        TempData["NewsSuccess"] = "Новината беше публикувана.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = "Principal")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(
        Guid newsId,
        string title,
        string text,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await newsService.UpdateNewsAsync(newsId, title, text, cancellationToken);
            TempData["NewsSuccess"] = "Новината беше обновена.";
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            TempData["NewsError"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = "Principal")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(
        Guid newsId,
        CancellationToken cancellationToken = default)
    {
        await newsService.DeleteNewsAsync(newsId, cancellationToken);
        TempData["NewsSuccess"] = "Новината беше изтрита.";

        return RedirectToAction(nameof(Index));
    }

    private async Task<NewsIndexViewModel> BuildViewModelAsync(CancellationToken cancellationToken)
    {
        var news = await newsService.GetAllNewsAsync(cancellationToken);

        return new NewsIndexViewModel
        {
            News = news
                .Select(newsItem => new NewsItemViewModel
                {
                    Id = newsItem.Id,
                    Title = newsItem.Title,
                    Text = newsItem.Text,
                    PreviewText = CreatePreviewText(newsItem.Text),
                    CreatedAt = newsItem.CreatedAt,
                    CreatedAtLabel = newsItem.CreatedAt
                        .ToLocalTime()
                        .ToString("dd.MM.yyyy HH:mm"),
                })
                .ToList(),
        };
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return userIdClaim is not null && Guid.TryParse(userIdClaim.Value, out var userId)
            ? userId
            : null;
    }

    private static string CreatePreviewText(string text)
    {
        var singleLineText = text
            .Replace("\r", " ")
            .Replace("\n", " ")
            .Trim();

        return singleLineText.Length <= NewsPreviewLength
            ? singleLineText
            : $"{singleLineText[..NewsPreviewLength].TrimEnd()}...";
    }
}
