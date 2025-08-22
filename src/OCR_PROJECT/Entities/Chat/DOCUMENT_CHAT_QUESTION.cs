using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Document.Intelligence.Agent.Entities.Chat;

/// <summary>
/// 사용자 잘의 기록
/// TODO: 페이징 필수
/// </summary>
public class DOCUMENT_CHAT_QUESTION : DOCUMENT_ENTITY_BASE
{
    /// <summary>
    /// 문서 CHAT 마스터 ID
    /// </summary>
    public Guid ThreadId { get; set; }
    /// <summary>
    /// 문서 CHAT 마스터
    /// </summary>
    public virtual DOCUMENT_CHAT_THREAD ChatThread { get; set; }
    
    /// <summary>
    /// KEY
    /// </summary>
    public Guid Id { get; set; }    
    /// <summary>
    /// 사용자 질의
    /// </summary>
    public string Question { get; set; }
    /// <summary>
    /// 사용자 질의 벡터 (SQL SERVER 2025부터 벡터 검색 지원)
    /// </summary>
    public float[] QuestionVector { get; set; }
    public string QueryPlan { get; set; }
    public string[] ChunkIdList { get; set; }
    /// <summary>
    /// 응답에 사용된 데이터셋
    /// </summary>
    public virtual ICollection<DOCUMENT_CHAT_QUESTION_RESEARCH> QuestionSearches { get; set; }
    /// <summary>
    /// 응답
    /// </summary>
    public virtual ICollection<DOCUMENT_CHAT_ANSWER> Answers { get; set; }
}

public class DocumentChatQuestionEntityConfiguration: IEntityTypeConfiguration<DOCUMENT_CHAT_QUESTION>
{
    public void Configure(EntityTypeBuilder<DOCUMENT_CHAT_QUESTION> builder)
    {
        builder.ToTable($"{nameof(DOCUMENT_CHAT_QUESTION)}", "dbo");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id)
            .ValueGeneratedOnAdd();

        //실제 저장은 binary로 되지만 조회시 float[]로 변경.
        builder.Property(x => x.QuestionVector)
            .HasColumnType("varbinary(max)")
            .HasConversion(
                v => FloatArrayToBytes(v),
                v => BytesToFloatArray(v))
            .Metadata.SetValueComparer(FloatArrayComparer);
        
        var jsonOpts = new JsonSerializerOptions { WriteIndented = false };
        var chunkIdListConverter = new ValueConverter<string[], string>(
            v => JsonSerializer.Serialize(v ?? Array.Empty<string>(), jsonOpts),
            v => string.IsNullOrWhiteSpace(v)
                ? Array.Empty<string>()
                : (JsonSerializer.Deserialize<string[]>(v, jsonOpts) ?? Array.Empty<string>()));

        // JSON 배열(ChunkIdList)도 동일 패턴 권장
        var strArrayComparer = new ValueComparer<string[]>(
            (a,b) => a != null && b != null && a.SequenceEqual(b),
            v => v == null ? 0 : v.Aggregate(0, (acc, x) => HashCode.Combine(acc, x.GetHashCode())),
            v => v == null ? null : v.ToArray());

        builder.Property(m => m.ChunkIdList)
            .HasColumnType("nvarchar(max)")
            .HasConversion(chunkIdListConverter)
            .Metadata.SetValueComparer(strArrayComparer);
        
        builder.HasIndex(q => new { q.ThreadId, q.CreatedAt });
        
        builder.HasOne(m => m.ChatThread)
            .WithMany(m => m.Questions)
            .HasForeignKey(m => m.ThreadId)
            .OnDelete(DeleteBehavior.Cascade);
    }
    
    static readonly ValueComparer<float[]> FloatArrayComparer = new(
        (a,b) => a != null && b != null && a.SequenceEqual(b),
        v => v == null ? 0 : v.Aggregate(0, (acc,x) => HashCode.Combine(acc, x.GetHashCode())),
        v => v == null ? null : v.ToArray());

    static byte[] FloatArrayToBytes(float[] arr)
    {
        if (arr == null || arr.Length == 0) return [];
        var bytes = new byte[arr.Length * sizeof(float)];
        Buffer.BlockCopy(arr, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    static float[] BytesToFloatArray(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0) return [];
        var arr = new float[bytes.Length / sizeof(float)];
        Buffer.BlockCopy(bytes, 0, arr, 0, bytes.Length);
        return arr;
    }
}