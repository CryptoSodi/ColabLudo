using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;

namespace Ludo.Api.Controllers;

[ApiController]
[Route("api/webrtc")]
public class WebRtcController : ControllerBase
{
    private static readonly ConcurrentDictionary<string, WebRtcOfferEnvelope> OffersByRoomAndPlayer = new(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, WebRtcAnswerEnvelope> AnswersByRoomTargetAndResponder = new(StringComparer.OrdinalIgnoreCase);

    [HttpPost("offers")]
    public ActionResult<WebRtcOfferEnvelope> UpsertOffer([FromBody] WebRtcOfferUpsertRequest request)
    {
        if (request == null ||
            string.IsNullOrWhiteSpace(request.RoomId) ||
            string.IsNullOrWhiteSpace(request.PlayerColor) ||
            string.IsNullOrWhiteSpace(request.OffersJson))
            return BadRequest("roomId, playerColor, and offersJson are required.");

        var roomId = request.RoomId.Trim();
        var playerColor = NormalizeColor(request.PlayerColor);
        var envelope = new WebRtcOfferEnvelope(roomId, playerColor, request.OffersJson, DateTime.UtcNow);
        OffersByRoomAndPlayer[$"{roomId}:{playerColor}"] = envelope;
        Console.WriteLine($"[WebRTC][API] Offer upserted. Room={roomId}, PlayerColor={playerColor}, Length={request.OffersJson.Length}");
        return envelope;
    }

    [HttpGet("offers")]
    public ActionResult<List<WebRtcOfferEnvelope>> GetOffers([FromQuery] string roomId)
    {
        if (string.IsNullOrWhiteSpace(roomId))
            return BadRequest("roomId is required.");

        var rid = roomId.Trim();
        var offers = OffersByRoomAndPlayer.Values
            .Where(x => x.RoomId.Equals(rid, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(x => x.UpdatedUtc)
            .Take(8)
            .ToList();
        Console.WriteLine($"[WebRTC][API] Offers fetched. Room={rid}, Count={offers.Count}");
        return offers;
    }

    [HttpPost("answers")]
    public ActionResult<WebRtcAnswerEnvelope> UpsertAnswer([FromBody] WebRtcAnswerUpsertRequest request)
    {
        if (request == null ||
            string.IsNullOrWhiteSpace(request.RoomId) ||
            string.IsNullOrWhiteSpace(request.PlayerColor) ||
            string.IsNullOrWhiteSpace(request.TargetPlayerColor) ||
            string.IsNullOrWhiteSpace(request.AnswerJson))
            return BadRequest("roomId, playerColor, targetPlayerColor, and answerJson are required.");

        var roomId = request.RoomId.Trim();
        var responder = NormalizeColor(request.PlayerColor);
        var target = NormalizeColor(request.TargetPlayerColor);
        var envelope = new WebRtcAnswerEnvelope(roomId, target, responder, request.AnswerJson, DateTime.UtcNow);
        AnswersByRoomTargetAndResponder[$"{roomId}:{target}:{responder}"] = envelope;
        Console.WriteLine($"[WebRTC][API] Answer upserted. Room={roomId}, Target={target}, Responder={responder}, Length={request.AnswerJson.Length}");
        return envelope;
    }

    [HttpGet("answers")]
    public ActionResult<List<WebRtcAnswerEnvelope>> GetAnswers([FromQuery] string roomId, [FromQuery] string playerColor)
    {
        if (string.IsNullOrWhiteSpace(roomId) || string.IsNullOrWhiteSpace(playerColor))
            return BadRequest("roomId and playerColor are required.");

        var rid = roomId.Trim();
        var target = NormalizeColor(playerColor);
        var answers = AnswersByRoomTargetAndResponder.Values
            .Where(x =>
                x.RoomId.Equals(rid, StringComparison.OrdinalIgnoreCase) &&
                x.TargetPlayerColor.Equals(target, StringComparison.Ordinal))
            .OrderByDescending(x => x.UpdatedUtc)
            .Take(8)
            .ToList();
        Console.WriteLine($"[WebRTC][API] Answers fetched. Room={rid}, Target={target}, Count={answers.Count}");
        return answers;
    }

    private static string NormalizeColor(string color)
    {
        return color.Trim().ToLowerInvariant();
    }
}

public record WebRtcOfferUpsertRequest(string RoomId, string PlayerColor, string OffersJson);
public record WebRtcOfferEnvelope(string RoomId, string PlayerColor, string OffersJson, DateTime UpdatedUtc);
public record WebRtcAnswerUpsertRequest(string RoomId, string PlayerColor, string TargetPlayerColor, string AnswerJson);
public record WebRtcAnswerEnvelope(string RoomId, string TargetPlayerColor, string PlayerColor, string AnswerJson, DateTime UpdatedUtc);
