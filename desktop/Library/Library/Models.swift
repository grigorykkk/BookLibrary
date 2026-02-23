import Foundation

struct Author: Identifiable, Codable, Hashable {
    let id: Int
    var firstName: String
    var lastName: String
    var birthDate: String
    var country: String?

    var fullName: String {
        let value = "\(firstName) \(lastName)".trimmingCharacters(in: .whitespacesAndNewlines)
        return value.isEmpty ? "Unknown author" : value
    }
}

struct AuthorRequest: Codable {
    var firstName: String
    var lastName: String
    var birthDate: String
    var country: String?
}

struct Genre: Identifiable, Codable, Hashable {
    let id: Int
    var name: String
    var description: String?
}

struct GenreRequest: Codable {
    var name: String
    var description: String?
}

struct Book: Identifiable, Codable, Hashable {
    let id: Int
    var title: String
    var authorId: Int
    var authorName: String
    var genreId: Int
    var genreName: String
    var publishYear: Int
    var isbn: String
    var quantityInStock: Int
}

struct BookRequest: Codable {
    var title: String
    var authorId: Int
    var genreId: Int
    var publishYear: Int
    var isbn: String
    var quantityInStock: Int
}

enum DateOnlyFormatter {
    private static let formatter: DateFormatter = {
        let dateFormatter = DateFormatter()
        dateFormatter.dateFormat = "yyyy-MM-dd"
        dateFormatter.calendar = Calendar(identifier: .gregorian)
        dateFormatter.locale = Locale(identifier: "en_US_POSIX")
        dateFormatter.timeZone = TimeZone(secondsFromGMT: 0)
        return dateFormatter
    }()

    static func string(from date: Date) -> String {
        formatter.string(from: date)
    }

    static func date(from value: String) -> Date? {
        formatter.date(from: value)
    }
}
