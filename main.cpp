#include <bits/stdc++.h>

using namespace std;

typedef long long ll;
typedef long double ld;

void print_all(int *a, int n) {
    for (int i = 0; i < n; i++) {
        cout << a[i] << " ";
    }
    cout << endl;
}

void bubble_sort(int* a, int n) {
    for (int i = 0; i < n - 1; i++) {
        for (int j = 0; j < n -i- 1; j++) {
            if (a[j] > a[j + 1]) {
                int temp = a[j];
                a[j] = a[j+1];
                a[j + 1] = temp;
                print_all(a, n);
            }
        }
    }
}

void insert_sort(int* a, int n) {
    int k, j;
    for (int i = 1; i < n; ++i) {
        k = a[i];
        j = i - 1;
        while (j >= 0 && a[j] > k) {
            a[j + 1] = a[j];
            j = j - 1;
        }
        a[j + 1] = k;
        print_all(a, n);
    }
}

void selection_sort(int* a, int n) {
    for (int i = 0; i < n - 1; i++) {
        int min_i = i;
        for (int j = i + 1; j < n; j++) {
            if (a[j] < a[min_i]) {
                min_i = j;
            }
        }
        if (min_i != i) {
            int temp = a[i];
            a[i] = a[min_i];
            a[min_i] = temp;
            print_all(a, n);
        }
    }
}

int main() {
    int n, c, i1 = 0;
    cin >> n;
    int arr[n];
    c = 1;
    for (int i = 0; i < n; ++i) {
        cin >> arr[i];
    }
    insert_sort(arr, n);
    for (auto v : arr) {
        cout << v << " ";
    }

}