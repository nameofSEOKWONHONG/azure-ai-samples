namespace Document.Intelligence.Agent.Features.Topic.Models;

/// <summary>
/// 토픽 생성
/// </summary>
/// <param name="TopicName">이름이자 Filter명이 된다.</param>
/// <param name="ObjectItems"></param>
public sealed record CreateTopicRequest(Guid? Id, string TopicName, string Category, ObjectItem[] ObjectItems);

/// <summary>
/// Graph SDK에서 조회된 내역 (파일 또는 폴더)
/// </summary>
/// <param name="DriveId"></param>
/// <param name="ItemId"></param>
/// <param name="IsFolder">false:파일, true:폴더</param>
public sealed record ObjectItem(string Path, string DriveId, string ItemId, bool IsFolder);

/// <summary>
/// MQ에 전달할 ObjectItem
/// </summary>
/// <param name="DriveId"></param>
/// <param name="ItemId"></param>
/// <param name="IsFolder">false:파일, true:폴더</param>
public sealed record MqObjectItem(string TopicId, string TopicName, string MetadataId, string Path, string DriveId, string ItemId, bool IsFolder);