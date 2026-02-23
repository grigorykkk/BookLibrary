import SwiftUI

struct BookFormView: View {
    let title: String
    let book: Book?
    let authors: [Author]
    let genres: [Genre]
    let onSave: (BookRequest) async -> Bool

    @Environment(\.dismiss) private var dismiss

    @State private var bookTitle: String
    @State private var selectedAuthorId: Int
    @State private var selectedGenreId: Int
    @State private var publishYear: String
    @State private var isbn: String
    @State private var quantityInStock: String
    @State private var localErrorMessage: String?
    @State private var isSaving = false

    init(
        title: String,
        book: Book?,
        authors: [Author],
        genres: [Genre],
        onSave: @escaping (BookRequest) async -> Bool)
    {
        self.title = title
        self.book = book
        self.authors = authors
        self.genres = genres
        self.onSave = onSave

        let defaultAuthorId = book?.authorId ?? authors.first?.id ?? 0
        let defaultGenreId = book?.genreId ?? genres.first?.id ?? 0

        _bookTitle = State(initialValue: book?.title ?? "")
        _selectedAuthorId = State(initialValue: defaultAuthorId)
        _selectedGenreId = State(initialValue: defaultGenreId)
        _publishYear = State(initialValue: book.map { String($0.publishYear) } ?? "")
        _isbn = State(initialValue: book?.isbn ?? "")
        _quantityInStock = State(initialValue: book.map { String($0.quantityInStock) } ?? "0")
    }

    var body: some View {
        NavigationStack {
            Form {
                Section("Основные данные") {
                    TextField("Название", text: $bookTitle)
                    TextField("ISBN", text: $isbn)
                    TextField("Год публикации", text: $publishYear)
                    TextField("Количество в наличии", text: $quantityInStock)
                }

                Section("Связи") {
                    if authors.isEmpty {
                        Text("Нет авторов. Сначала добавьте автора.")
                            .foregroundStyle(.secondary)
                    } else {
                        Picker("Автор", selection: $selectedAuthorId) {
                            ForEach(authors) { author in
                                Text(author.fullName).tag(author.id)
                            }
                        }
                    }

                    if genres.isEmpty {
                        Text("Нет жанров. Сначала добавьте жанр.")
                            .foregroundStyle(.secondary)
                    } else {
                        Picker("Жанр", selection: $selectedGenreId) {
                            ForEach(genres) { genre in
                                Text(genre.name).tag(genre.id)
                            }
                        }
                    }
                }

                if let localErrorMessage {
                    Section {
                        Text(localErrorMessage)
                            .foregroundStyle(.red)
                    }
                }
            }
            .navigationTitle(title)
            .toolbar {
                ToolbarItem(placement: .cancellationAction) {
                    Button("Отмена") {
                        dismiss()
                    }
                }

                ToolbarItem(placement: .confirmationAction) {
                    Button(isSaving ? "Сохранение..." : "Сохранить") {
                        Task {
                            await save()
                        }
                    }
                    .disabled(isSaving || authors.isEmpty || genres.isEmpty)
                }
            }
        }
    }

    private func save() async {
        localErrorMessage = nil

        let normalizedTitle = bookTitle.trimmingCharacters(in: .whitespacesAndNewlines)
        let normalizedIsbn = isbn.trimmingCharacters(in: .whitespacesAndNewlines)

        guard !normalizedTitle.isEmpty else {
            localErrorMessage = "Название обязательно."
            return
        }

        guard !normalizedIsbn.isEmpty else {
            localErrorMessage = "ISBN обязателен."
            return
        }

        guard let normalizedPublishYear = Int(publishYear), (1...9999).contains(normalizedPublishYear) else {
            localErrorMessage = "Год публикации должен быть числом от 1 до 9999."
            return
        }

        guard let normalizedQuantityInStock = Int(quantityInStock), normalizedQuantityInStock >= 0 else {
            localErrorMessage = "Количество в наличии не может быть отрицательным."
            return
        }

        guard selectedAuthorId > 0 else {
            localErrorMessage = "Выберите автора."
            return
        }

        guard selectedGenreId > 0 else {
            localErrorMessage = "Выберите жанр."
            return
        }

        let request = BookRequest(
            title: normalizedTitle,
            authorId: selectedAuthorId,
            genreId: selectedGenreId,
            publishYear: normalizedPublishYear,
            isbn: normalizedIsbn,
            quantityInStock: normalizedQuantityInStock)

        isSaving = true
        let isSaved = await onSave(request)
        isSaving = false

        if isSaved {
            dismiss()
        }
    }
}
