import SwiftUI

struct GenresView: View {
    @ObservedObject var store: LibraryStore

    @State private var isPresentingCreateGenre = false
    @State private var editingGenre: Genre?
    @State private var deletingGenre: Genre?

    var body: some View {
        NavigationStack {
            List(store.genres) { genre in
                VStack(alignment: .leading, spacing: 4) {
                    Text(genre.name)
                        .font(.headline)

                    if let description = genre.description, !description.isEmpty {
                        Text(description)
                            .font(.footnote)
                            .foregroundStyle(.secondary)
                    }
                }
                .contextMenu {
                    Button("Редактировать") {
                        editingGenre = genre
                    }

                    Button("Удалить", role: .destructive) {
                        deletingGenre = genre
                    }
                }
            }
            .overlay {
                if store.genres.isEmpty {
                    ContentUnavailableView("Нет жанров", systemImage: "tag")
                }
            }
            .navigationTitle("Genres")
            .toolbar {
                ToolbarItem(placement: .automatic) {
                    Button("Обновить") {
                        Task {
                            await store.refreshReferences()
                        }
                    }
                }

                ToolbarItem(placement: .primaryAction) {
                    Button("Добавить") {
                        isPresentingCreateGenre = true
                    }
                }
            }
            .sheet(isPresented: $isPresentingCreateGenre) {
                GenreFormView(title: "Новый жанр", genre: nil) { request in
                    await store.createGenre(request: request)
                }
            }
            .sheet(item: $editingGenre) { genre in
                GenreFormView(title: "Редактировать жанр", genre: genre) { request in
                    await store.updateGenre(id: genre.id, request: request)
                }
            }
            .alert("Удалить жанр?", isPresented: Binding(
                get: { deletingGenre != nil },
                set: { isPresented in
                    if !isPresented {
                        deletingGenre = nil
                    }
                }))
            {
                Button("Удалить", role: .destructive) {
                    guard let deletingGenre else {
                        return
                    }

                    Task {
                        _ = await store.deleteGenre(id: deletingGenre.id)
                        self.deletingGenre = nil
                    }
                }

                Button("Отмена", role: .cancel) {
                    deletingGenre = nil
                }
            } message: {
                Text("Если у жанра есть книги, сервер вернёт ошибку.")
            }
        }
    }
}
