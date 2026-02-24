import SwiftUI

struct BooksView: View {
    @ObservedObject var store: LibraryStore

    @State private var isPresentingCreateBook = false
    @State private var editingBook: Book?
    @State private var deletingBook: Book?

    var body: some View {
        NavigationStack {
            VStack(spacing: 12) {
                filtersSection

                if store.isBooksLoading && store.books.isEmpty {
                    ProgressView("Загрузка книг...")
                        .frame(maxWidth: .infinity, maxHeight: .infinity, alignment: .center)
                } else {
                    List(store.books) { book in
                        VStack(alignment: .leading, spacing: 4) {
                            Text(book.title)
                                .font(.headline)

                            Text("Автор: \(book.authorNames.joined(separator: ", ")) • Жанр: \(book.genreName)")
                                .foregroundStyle(.secondary)

                            Text("Год: \(book.publishYear) • ISBN: \(book.isbn) • В наличии: \(book.quantityInStock)")
                                .font(.footnote)
                                .foregroundStyle(.secondary)
                        }
                        .contextMenu {
                            Button("Редактировать") {
                                editingBook = book
                            }

                            Button("Удалить", role: .destructive) {
                                deletingBook = book
                            }
                        }
                    }
                    .overlay {
                        if store.books.isEmpty && !store.isBooksLoading {
                            ContentUnavailableView("Нет книг", systemImage: "books.vertical")
                        }
                    }
                }
            }
            .padding(.horizontal)
            .padding(.top, 8)
            .navigationTitle("Books")
            .toolbar {
                ToolbarItem(placement: .automatic) {
                    Button("Обновить") {
                        Task {
                            await store.refreshReferences()
                            await store.loadBooks()
                        }
                    }
                }

                ToolbarItem(placement: .primaryAction) {
                    Button("Добавить") {
                        isPresentingCreateBook = true
                    }
                }
            }
            .sheet(isPresented: $isPresentingCreateBook) {
                BookFormView(
                    title: "Новая книга",
                    book: nil,
                    authors: store.authors,
                    genres: store.genres)
                { request in
                    await store.createBook(request: request)
                }
            }
            .sheet(item: $editingBook) { book in
                BookFormView(
                    title: "Редактировать книгу",
                    book: book,
                    authors: store.authors,
                    genres: store.genres)
                { request in
                    await store.updateBook(id: book.id, request: request)
                }
            }
            .alert("Удалить книгу?", isPresented: Binding(
                get: { deletingBook != nil },
                set: { isPresented in
                    if !isPresented {
                        deletingBook = nil
                    }
                }))
            {
                Button("Удалить", role: .destructive) {
                    guard let deletingBook else {
                        return
                    }

                    Task {
                        _ = await store.deleteBook(id: deletingBook.id)
                        self.deletingBook = nil
                    }
                }

                Button("Отмена", role: .cancel) {
                    deletingBook = nil
                }
            } message: {
                Text("Действие нельзя отменить.")
            }
        }
    }

    private var filtersSection: some View {
        VStack(alignment: .leading, spacing: 10) {
            TextField("Поиск по названию", text: $store.searchText)
                .textFieldStyle(.roundedBorder)
                .onSubmit {
                    Task {
                        await store.loadBooks()
                    }
                }

            HStack {
                Picker("Автор", selection: $store.selectedAuthorId) {
                    Text("Все авторы").tag(Optional<Int>.none)
                    ForEach(store.authors) { author in
                        Text(author.fullName).tag(Optional<Int>(author.id))
                    }
                }

                Picker("Жанр", selection: $store.selectedGenreId) {
                    Text("Все жанры").tag(Optional<Int>.none)
                    ForEach(store.genres) { genre in
                        Text(genre.name).tag(Optional<Int>(genre.id))
                    }
                }
            }

            HStack {
                Button("Применить фильтры") {
                    Task {
                        await store.loadBooks()
                    }
                }

                Button("Сбросить") {
                    store.resetBookFilters()
                    Task {
                        await store.loadBooks()
                    }
                }
            }
        }
    }
}
