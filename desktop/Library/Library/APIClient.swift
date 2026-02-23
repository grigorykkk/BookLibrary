import Foundation

enum APIError: LocalizedError {
    case invalidURL
    case invalidResponse
    case transport(String)
    case server(statusCode: Int, message: String)
    case decoding(String)

    var errorDescription: String? {
        switch self {
        case .invalidURL:
            return "Некорректный URL API."
        case .invalidResponse:
            return "Некорректный ответ сервера."
        case .transport(let message):
            return "Ошибка сети: \(message)"
        case .server(_, let message):
            return message
        case .decoding(let message):
            return "Ошибка обработки ответа: \(message)"
        }
    }
}

private struct EmptyResponse: Decodable {}

private struct ServerErrorEnvelope: Decodable {
    let message: String?
    let title: String?
    let detail: String?
    let errors: [String: [String]]?

    var readableMessage: String {
        if let message, !message.isEmpty {
            return message
        }

        if let errors, !errors.isEmpty {
            let lines = errors
                .sorted(by: { $0.key < $1.key })
                .flatMap { entry in entry.value.map { "\(entry.key): \($0)" } }
            if !lines.isEmpty {
                return lines.joined(separator: "\n")
            }
        }

        if let detail, !detail.isEmpty {
            return detail
        }

        if let title, !title.isEmpty {
            return title
        }

        return "Неизвестная ошибка сервера."
    }
}

final class APIClient {
    private let baseURL: URL
    private let session: URLSession

    init(
        baseURL: URL = URL(string: "http://localhost:5036")!,
        session: URLSession = .shared)
    {
        self.baseURL = baseURL
        self.session = session
    }

    func get<T: Decodable>(_ path: String, query: [URLQueryItem] = []) async throws -> T {
        try await send(path: path, method: "GET", query: query, body: nil)
    }

    func post<T: Decodable, Body: Encodable>(_ path: String, body: Body) async throws -> T {
        let data = try JSONEncoder().encode(body)
        return try await send(path: path, method: "POST", body: data)
    }

    func put<T: Decodable, Body: Encodable>(_ path: String, body: Body) async throws -> T {
        let data = try JSONEncoder().encode(body)
        return try await send(path: path, method: "PUT", body: data)
    }

    func delete(_ path: String) async throws {
        let _: EmptyResponse = try await send(path: path, method: "DELETE", body: nil)
    }

    private func send<T: Decodable>(
        path: String,
        method: String,
        query: [URLQueryItem] = [],
        body: Data?) async throws -> T
    {
        guard let url = buildURL(path: path, query: query) else {
            throw APIError.invalidURL
        }

        var request = URLRequest(url: url)
        request.httpMethod = method
        request.httpBody = body

        if body != nil {
            request.setValue("application/json", forHTTPHeaderField: "Content-Type")
        }

        request.setValue("application/json", forHTTPHeaderField: "Accept")

        let result: (Data, URLResponse)
        do {
            result = try await session.data(for: request)
        } catch {
            throw APIError.transport(error.localizedDescription)
        }

        guard let httpResponse = result.1 as? HTTPURLResponse else {
            throw APIError.invalidResponse
        }

        let statusCode = httpResponse.statusCode
        let data = result.0

        guard (200...299).contains(statusCode) else {
            let message = decodeServerMessage(from: data)
            throw APIError.server(statusCode: statusCode, message: "HTTP \(statusCode): \(message)")
        }

        if T.self == EmptyResponse.self && data.isEmpty {
            return EmptyResponse() as! T
        }

        if data.isEmpty {
            throw APIError.invalidResponse
        }

        do {
            return try JSONDecoder().decode(T.self, from: data)
        } catch {
            throw APIError.decoding(error.localizedDescription)
        }
    }

    private func buildURL(path: String, query: [URLQueryItem]) -> URL? {
        let normalizedPath = path.hasPrefix("/") ? String(path.dropFirst()) : path
        let combinedURL = baseURL.appending(path: normalizedPath)
        guard var components = URLComponents(url: combinedURL, resolvingAgainstBaseURL: false) else {
            return nil
        }

        components.queryItems = query.isEmpty ? nil : query
        return components.url
    }

    private func decodeServerMessage(from data: Data) -> String {
        guard !data.isEmpty else {
            return "Сервер вернул ошибку без сообщения."
        }

        if let envelope = try? JSONDecoder().decode(ServerErrorEnvelope.self, from: data) {
            return envelope.readableMessage
        }

        if let raw = String(data: data, encoding: .utf8), !raw.isEmpty {
            return raw
        }

        return "Сервер вернул ошибку без сообщения."
    }
}
