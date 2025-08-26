using DocumentFormat.OpenXml.Packaging;
using eXtensionSharp;
using Microsoft.Graph;
#pragma warning disable CS8602 // null 가능 참조에 대한 역참조입니다.

namespace Document.Intelligence.Agent.Test;

public class GraphApiSample
{
    private readonly GraphServiceClient _graph;

    public GraphApiSample(GraphServiceClient graph)
    {
        this._graph = graph;
    }

    public async Task RunAsync()
    {
        var sites = await _graph.Sites.GetAsync(m =>
        {
            m.QueryParameters.Search = "gowitco";
            m.QueryParameters.Top = 50;
            m.QueryParameters.Select = ["id","name","displayName","webUrl","siteCollection"];
        });
        var selectedSite = sites!.Value!.First(m => m.DisplayName == "CS팀");
        var drives = await _graph.Sites[selectedSite.Id].Drives.GetAsync(r =>
        {
            r.QueryParameters.Select = ["id", "name", "driveType", "webUrl"];
            r.QueryParameters.Top = 50;
        });
        var selectedDrive = drives!.Value!.First(m => m.Name == "문서");
        var items = await _graph.Drives[selectedDrive.Id].Items["root"].Children
            .GetAsync(r =>
            {
                r.QueryParameters.Top = 200;
            });

        if (items.xIsNotEmpty())
        {
            var files = items?.Value?.Where(m => m.File != null).ToList();
            var selectedFiles = files?.Where(m =>
                m.File?.MimeType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document" ||
                m.File?.MimeType == "application/msword").ToList();
            if(selectedFiles.xIsNotEmpty())
            {
                var driveId = selectedFiles[0]?.ParentReference?.DriveId;
                var itemId = selectedFiles[0].Id;
                var item = await _graph.Drives[driveId].Items[itemId].GetAsync(r => r.QueryParameters.Select =
                    ["name", "file", "size", "fileSystemInfo"]);

                await using (var content = await _graph.Drives[driveId].Items[itemId].Content.GetAsync())
                {
                    await using (var fs = File.Create($"./{selectedFiles[0].Name}"))
                    {
                        await content!.CopyToAsync(fs);    
                    }
                }
                
                await using var stream = File.OpenRead($"./{selectedFiles[0].Name}");
                using var doc = WordprocessingDocument.Open(stream, false);
                var text = doc.MainDocumentPart?.Document?.Body?.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>();
                if (text.xIsNotEmpty())
                {
                    
                }
            }
        }
    }
}