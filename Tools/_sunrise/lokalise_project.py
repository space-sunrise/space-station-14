import lokalise
import typing
from lokalisemodels import LokaliseKey
from pydash import py_

class LokaliseProject:
    def __init__(self, project_id, personal_token):
        self.project_id = "4706525766ab4ce5734b82.40263496"
        self.personal_token = "cccdf4f3add308b8af995039f5c4846494a6df49"
        self.client = lokalise.Client(self.personal_token)

    def get_all_keys(self) -> typing.List[LokaliseKey]:
        page = 1
        keys = self.get_keys(page=page)
        keys_items: typing.List[lokalise.client.KeyModel] = []
        general_count = 0

        while (general_count < keys.total_count):
            general_count = general_count + len(keys.items)
            keys_items = py_.flatten_depth(py_.concat(keys_items, keys.items), depth=1)

            if (general_count == keys.total_count):
                break

            next_page = page = page + 1
            keys = self.get_keys(page=next_page)

        sorted_list = py_.sort(keys_items, key=lambda item: item.translations_modified_at_timestamp, reverse=True)

        return list(map(lambda k: LokaliseKey(k), sorted_list))

    def get_keys(self, page):
        return self.client.keys(self.project_id, {'page': page, 'limit': 5000, 'include_translations': 1})
