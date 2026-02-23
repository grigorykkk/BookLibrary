//
//  ContentView.swift
//  Library
//
//  Created by Григорий Костин on 11.02.2026.
//

import SwiftUI

struct ContentView: View {
    @StateObject private var store = LibraryStore()

    var body: some View {
        TabView {
            BooksView(store: store)
                .tabItem {
                    Label("Books", systemImage: "books.vertical")
                }

            AuthorsView(store: store)
                .tabItem {
                    Label("Authors", systemImage: "person.2")
                }

            GenresView(store: store)
                .tabItem {
                    Label("Genres", systemImage: "tag")
                }
        }
        .task {
            await store.loadInitialData()
        }
        .alert("Ошибка", isPresented: Binding(
            get: { store.errorMessage != nil },
            set: { isPresented in
                if !isPresented {
                    store.clearError()
                }
            }))
        {
            Button("OK", role: .cancel) {
                store.clearError()
            }
        } message: {
            Text(store.errorMessage ?? "")
        }
    }
}

#Preview {
    ContentView()
}
