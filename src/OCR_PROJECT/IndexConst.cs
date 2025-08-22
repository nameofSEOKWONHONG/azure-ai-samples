namespace Document.Intelligence.Agent;

/// <summary>
/// AI SEARCH INDEX 목록
/// </summary>
public class INDEX_CONST
{
    /// <summary>
    /// 영수증
    /// </summary>
    public const string RECEIPT_INDEX = nameof(RECEIPT_INDEX);
    
    /// <summary>
    /// 문서
    /// </summary>
    public const string DOCUMENT_INDEX = nameof(DOCUMENT_INDEX);
}

/// <summary>
/// TODO: 사용자 지정 카테고리를 사용한다면 사용하지 않음.
/// </summary>
public class DOCUMENT_CATEGORY_CONST
{
    /// <summary>
    /// 인사
    /// </summary>
    public const string HUMAN_RESOURCES = nameof(HUMAN_RESOURCES);

    /// <summary>
    /// 복지
    /// </summary>
    public const string EMPLOYEE_BENEFITS = nameof(EMPLOYEE_BENEFITS);

    /// <summary>
    /// 보안
    /// </summary>
    public const string SECURITY = nameof(SECURITY);

    /// <summary>
    /// 권한
    /// </summary>
    public const string PERMISSIONS = nameof(PERMISSIONS);
}