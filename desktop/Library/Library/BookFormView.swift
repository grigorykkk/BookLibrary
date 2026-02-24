import SwiftUI

struct BookFormView: View {
    let title: String
    let book: Book?
    let authors: [Author]
    let genres: [Genre]
    let onSave: (BookRequest) async -> Bool

    @Environment(\.dismiss) private var dismiss

    @State private var bookTitle: String
    @State private var selectedAuthorIds: Set<Int>
    @State private var selectedGenreIds: Set<Int>
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

        let defaultAuthorIds: Set<Int> = {
            if let ids = book?.authorIds, !ids.isEmpty {
                return Set(ids)
            }
            return []
        }()
        let defaultGenreIds: Set<Int> = {
            if let ids = book?.genreIds, !ids.isEmpty {
                return Set(ids)
            }
            return []
        }()

        _bookTitle = State(initialValue: book?.title ?? "")
        _selectedAuthorIds = State(initialValue: defaultAuthorIds)
        _selectedGenreIds = State(initialValue: defaultGenreIds)
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
                        .onChange(of: isbn) { _, newValue in
                            let filtered = String(newValue.filter(\.isNumber).prefix(13))
                            if filtered != newValue {
                                isbn = filtered
                            }
                        }

                    TextField("Год публикации", text: $publishYear)
                    TextField("Количество в наличии", text: $quantityInStock)
                }

                Section("Авторы") {
                    if authors.isEmpty {
                        Text("Нет авторов. Сначала добавьте автора.")
                            .foregroundStyle(.secondary)
                    } else {
                        ForEach(authors) { author in
                            Toggle(author.fullName, isOn: Binding(
                                get: { selectedAuthorIds.contains(author.id) },
                                set: { isSelected in
                                    if isSelected {
                                        selectedAuthorIds.insert(author.id)
                                    } else {
                                        selectedAuthorIds.remove(author.id)
                                    }
                                }
                            ))
                        }
                    }
                }

                Section("Жанры") {
                    if genres.isEmpty {
                        Text("Нет жанров. Сначала добавьте жанр.")
                            .foregroundStyle(.secondary)
                    } else {
                        ForEach(genres) { genre in
                            Toggle(genre.name, isOn: Binding(
                                get: { selectedGenreIds.contains(genre.id) },
                                set: { isSelected in
                                    if isSelected {
                                        selectedGenreIds.insert(genre.id)
                                    } else {
                                        selectedGenreIds.remove(genre.id)
                                    }
                                }
                            ))
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

        guard normalizedIsbn.allSatisfy(\.isNumber), (1...13).contains(normalizedIsbn.count) else {
            localErrorMessage = "ISBN должен содержать только цифры (1–13)."
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

        guard !selectedAuthorIds.isEmpty else {
            localErrorMessage = "Выберите хотя бы одного автора."
            return
        }

        guard !selectedGenreIds.isEmpty else {
            localErrorMessage = "Выберите хотя бы один жанр."
            return
        }

        let request = BookRequest(
            title: normalizedTitle,
            authorIds: Array(selectedAuthorIds),
            genreIds: Array(selectedGenreIds),
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
