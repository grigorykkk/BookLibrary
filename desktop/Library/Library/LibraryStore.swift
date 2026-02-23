import Foundation
import Combine

@MainActor
final class LibraryStore: ObservableObject {
    @Published var books: [Book] = []
    @Published var authors: [Author] = []
    @Published var genres: [Genre] = []

    @Published var searchText: String = ""
    @Published var selectedAuthorId: Int?
    @Published var selectedGenreId: Int?

    @Published var isBooksLoading = false
    @Published var errorMessage: String?

    private let apiClient: APIClient

    init(apiClient: APIClient = APIClient()) {
        self.apiClient = apiClient
    }

    func loadInitialData() async {
        await refreshReferences()
        await loadBooks()
    }

    func refreshReferences() async {
        do {
            let fetchedAuthors: [Author] = try await apiClient.get("/api/authors")
            let fetchedGenres: [Genre] = try await apiClient.get("/api/genres")
            authors = fetchedAuthors
            genres = fetchedGenres
        } catch {
            handle(error: error)
        }
    }

    func loadBooks() async {
        isBooksLoading = true
        defer { isBooksLoading = false }

        var queryItems: [URLQueryItem] = []
        let trimmedSearch = searchText.trimmingCharacters(in: .whitespacesAndNewlines)

        if !trimmedSearch.isEmpty {
            queryItems.append(URLQueryItem(name: "search", value: trimmedSearch))
        }

        if let selectedAuthorId {
            queryItems.append(URLQueryItem(name: "authorId", value: String(selectedAuthorId)))
        }

        if let selectedGenreId {
            queryItems.append(URLQueryItem(name: "genreId", value: String(selectedGenreId)))
        }

        do {
            books = try await apiClient.get("/api/books", query: queryItems)
        } catch {
            handle(error: error)
        }
    }

    func resetBookFilters() {
        searchText = ""
        selectedAuthorId = nil
        selectedGenreId = nil
    }

    func createBook(request: BookRequest) async -> Bool {
        do {
            let _: Book = try await apiClient.post("/api/books", body: request)
            await loadBooks()
            return true
        } catch {
            handle(error: error)
            return false
        }
    }

    func updateBook(id: Int, request: BookRequest) async -> Bool {
        do {
            let _: Book = try await apiClient.put("/api/books/\(id)", body: request)
            await loadBooks()
            return true
        } catch {
            handle(error: error)
            return false
        }
    }

    func deleteBook(id: Int) async -> Bool {
        do {
            try await apiClient.delete("/api/books/\(id)")
            await loadBooks()
            return true
        } catch {
            handle(error: error)
            return false
        }
    }

    func createAuthor(request: AuthorRequest) async -> Bool {
        do {
            let _: Author = try await apiClient.post("/api/authors", body: request)
            await refreshReferences()
            await loadBooks()
            return true
        } catch {
            handle(error: error)
            return false
        }
    }

    func updateAuthor(id: Int, request: AuthorRequest) async -> Bool {
        do {
            let _: Author = try await apiClient.put("/api/authors/\(id)", body: request)
            await refreshReferences()
            await loadBooks()
            return true
        } catch {
            handle(error: error)
            return false
        }
    }

    func deleteAuthor(id: Int) async -> Bool {
        do {
            try await apiClient.delete("/api/authors/\(id)")
            await refreshReferences()
            await loadBooks()
            return true
        } catch {
            handle(error: error)
            return false
        }
    }

    func createGenre(request: GenreRequest) async -> Bool {
        do {
            let _: Genre = try await apiClient.post("/api/genres", body: request)
            await refreshReferences()
            await loadBooks()
            return true
        } catch {
            handle(error: error)
            return false
        }
    }

    func updateGenre(id: Int, request: GenreRequest) async -> Bool {
        do {
            let _: Genre = try await apiClient.put("/api/genres/\(id)", body: request)
            await refreshReferences()
            await loadBooks()
            return true
        } catch {
            handle(error: error)
            return false
        }
    }

    func deleteGenre(id: Int) async -> Bool {
        do {
            try await apiClient.delete("/api/genres/\(id)")
            await refreshReferences()
            await loadBooks()
            return true
        } catch {
            handle(error: error)
            return false
        }
    }

    func clearError() {
        errorMessage = nil
    }

    private func handle(error: Error) {
        errorMessage = (error as? LocalizedError)?.errorDescription ?? error.localizedDescription
    }
}
