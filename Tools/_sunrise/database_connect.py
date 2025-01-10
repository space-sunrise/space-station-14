import sqlite3
from typing import List, Tuple

class LocalizationDB:
    def __init__(self, db_name: str = "database.db"):
        self.conn = sqlite3.connect(db_name)
        self.cursor = self.conn.cursor()
        self.create_tables()

    def create_tables(self):
        self.cursor.execute('''
            CREATE TABLE IF NOT EXISTS prototypes (
                id TEXT PRIMARY KEY,
                "en-US" TEXT NOT NULL,
                "ru-RU" TEXT NOT NULL,
                last_updated TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            )
        ''')
        self.cursor.execute('''
            CREATE TABLE IF NOT EXISTS strings (
                id TEXT PRIMARY KEY,
                "en-US" TEXT NOT NULL,
                "ru-RU" TEXT NOT NULL,
                last_updated TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            )
        ''')
        self.conn.commit()

    def insert_translation(self, table: str, id: str, en_value: str, ru_value: str):
        self.cursor.execute(f'''
            INSERT INTO {table} (id, "en-US", "ru-RU")
            VALUES (?, ?, ?)
        ''', (id, en_value, ru_value))
        self.conn.commit()

    def get_translation(self, table: str, id: str, locale: str) -> Tuple[str, str]:
        self.cursor.execute(f'''
            SELECT id, "{locale}" FROM {table} WHERE id = ?
        ''', (id,))
        return self.cursor.fetchone()

    def get_all_translations(self, table: str, locale: str) -> List[Tuple[str, str, str]]:
        self.cursor.execute(f'''
            SELECT id, "en-US", "ru-RU" FROM {table}
        ''')
        return self.cursor.fetchall()

    def update_translation(self, table: str, id: str, locale: str, value: str):
        self.cursor.execute(f'''
            UPDATE {table} SET "{locale}" = ?, last_updated = CURRENT_TIMESTAMP WHERE id = ?
        ''', (value, id))
        self.conn.commit()

    def get_last_updated(self, table: str, id: str) -> str:
        self.cursor.execute(f'''
            SELECT last_updated FROM {table} WHERE id = ?
        ''', (id,))
        result = self.cursor.fetchone()
        return result[0] if result else None

    def close(self):
        self.conn.close()
